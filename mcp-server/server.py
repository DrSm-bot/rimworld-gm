#!/usr/bin/env python3
"""
Rimworld Game Master MCP Server

Bridges MCP (Model Context Protocol) to the Rimworld mod's HTTP API.
"""

import asyncio
import httpx
from typing import Any
from mcp.server import Server
from mcp.types import Tool, TextContent

# Configuration
RIMWORLD_API_URL = "http://localhost:18800"
SERVER_NAME = "rimworld-gm"
SERVER_VERSION = "0.1.0"

# Initialize MCP server
server = Server(SERVER_NAME)


# ============================================================================
# Tool Definitions
# ============================================================================

@server.list_tools()
async def list_tools() -> list[Tool]:
    """Define available MCP tools."""
    return [
        Tool(
            name="rimworld_get_status",
            description="Get the current status of the Rimworld colony including colonists, resources, and threats",
            inputSchema={
                "type": "object",
                "properties": {
                    "include_colonists": {
                        "type": "boolean",
                        "description": "Include detailed colonist info",
                        "default": True
                    },
                    "include_resources": {
                        "type": "boolean",
                        "description": "Include resource counts",
                        "default": True
                    }
                }
            }
        ),
        Tool(
            name="rimworld_trigger_event",
            description="Trigger an event in Rimworld (raids, cargo drops, weather, etc.)",
            inputSchema={
                "type": "object",
                "properties": {
                    "event_type": {
                        "type": "string",
                        "enum": [
                            "raid", "manhunter", "cargo_pod", "wanderer",
                            "solar_flare", "toxic_fallout", "psychic_drone",
                            "trader", "inspiration"
                        ],
                        "description": "Type of event to trigger"
                    },
                    "intensity": {
                        "type": "string",
                        "enum": ["low", "medium", "high"],
                        "description": "Event intensity/difficulty",
                        "default": "medium"
                    },
                    "target_colonist": {
                        "type": "string",
                        "description": "Target colonist name (for inspiration events)"
                    }
                },
                "required": ["event_type"]
            }
        ),
        Tool(
            name="rimworld_send_message",
            description="Display a message to the player in Rimworld",
            inputSchema={
                "type": "object",
                "properties": {
                    "text": {
                        "type": "string",
                        "description": "Message text to display"
                    },
                    "style": {
                        "type": "string",
                        "enum": ["info", "positive", "negative", "dramatic"],
                        "default": "info"
                    }
                },
                "required": ["text"]
            }
        )
    ]


# ============================================================================
# Tool Implementations
# ============================================================================

@server.call_tool()
async def call_tool(name: str, arguments: dict[str, Any]) -> list[TextContent]:
    """Handle tool calls from MCP clients."""
    
    try:
        if name == "rimworld_get_status":
            result = await get_colony_status(arguments)
        elif name == "rimworld_trigger_event":
            result = await trigger_event(arguments)
        elif name == "rimworld_send_message":
            result = await send_message(arguments)
        else:
            result = {"error": f"Unknown tool: {name}"}
        
        return [TextContent(type="text", text=str(result))]
    
    except httpx.ConnectError:
        return [TextContent(
            type="text",
            text="Error: Could not connect to Rimworld. Is the game running with the mod enabled?"
        )]
    except Exception as e:
        return [TextContent(type="text", text=f"Error: {str(e)}")]


async def get_colony_status(args: dict) -> dict:
    """Fetch colony status from Rimworld mod."""
    async with httpx.AsyncClient() as client:
        response = await client.get(f"{RIMWORLD_API_URL}/state", timeout=5.0)
        return response.json()


async def trigger_event(args: dict) -> dict:
    """Trigger an event in Rimworld."""
    event_type = args.get("event_type")
    intensity = args.get("intensity", "medium")
    
    # Map intensity to point values
    intensity_points = {
        "low": 200,
        "medium": 500,
        "high": 1000
    }
    
    payload = {
        "event_type": event_type,
        "params": {
            "points": intensity_points.get(intensity, 500),
            "target_colonist": args.get("target_colonist")
        }
    }
    
    async with httpx.AsyncClient() as client:
        response = await client.post(
            f"{RIMWORLD_API_URL}/event",
            json=payload,
            timeout=5.0
        )
        return response.json()


async def send_message(args: dict) -> dict:
    """Send an in-game message."""
    payload = {
        "text": args.get("text"),
        "type": args.get("style", "info"),
        "duration": 5
    }
    
    async with httpx.AsyncClient() as client:
        response = await client.post(
            f"{RIMWORLD_API_URL}/message",
            json=payload,
            timeout=5.0
        )
        return response.json()


# ============================================================================
# Main Entry Point
# ============================================================================

async def main():
    """Run the MCP server."""
    from mcp.server.stdio import stdio_server
    
    async with stdio_server() as (read_stream, write_stream):
        await server.run(
            read_stream,
            write_stream,
            server.create_initialization_options()
        )


if __name__ == "__main__":
    asyncio.run(main())
