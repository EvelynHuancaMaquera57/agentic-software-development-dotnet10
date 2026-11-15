# Agentic Software Development with .NET 10

> My hands-on journey learning to build software with AI agents (Claude Code) and .NET 10.

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![Claude Code](https://img.shields.io/badge/Claude_Code-Agent-D97757)
![Status](https://img.shields.io/badge/status-in_progress-yellow)

## About

This repository contains my own projects, exercises and notes from the course **Agentic Software Development with .NET 10**.

The goal is to move from *using AI as a chatbot* to *working with AI agents* that read the codebase, plan, edit multiple files, run tests and verify their own work, while I stay in control of **what** gets built and review **how** it is built.

## What I'm learning

| Topic | Description |
| --- | --- |
| Chatbot vs. Copilot vs. Agent | Why an agent works *with* you instead of just answering or autocompleting |
| The agentic loop | Prompt → Plan → Tool → Evaluate → Done? (repeat until complete) |
| Agent tools | Read, Write/Edit, Bash, Glob, Grep, Task (subagents), WebFetch |
| Interfaces | Terminal (CLI), IDE (VS Code), Web, Desktop app |
| CLAUDE.md | Persistent project memory: stack, architecture, conventions |
| Skills | Reusable, on-demand procedures for specific tasks |
| Subagents | Specialized AI workers running in parallel with their own context |
| MCP | Connecting the agent to external tools (Jira, databases, Google Workspace) |
| Hooks | Event-driven scripts that always run (formatting, security checks) |
| Agent SDK | Controlling the agent programmatically from code |
| Spec-Driven Development | Requirements → plan → tests → implementation, one spec at a time |
| Context engineering | Giving the model the *right* context, not *more* context |

## Tech stack

- **Language / Framework:** C# · .NET 10
- **AI agent:** Claude Code (Opus / Sonnet models)
- **Editor:** Visual Studio Code + Claude Code extension
- **Version control:** Git · GitHub
- **Methodology:** Spec-Driven Development (GitHub Spec Kit)

## Repository structure

```
agentic-software-development-dotnet10/
├── session-01-intro-to-agents/
├── session-02-.../
├── session-03-.../
├── session-04-.../
├── ...
├── notes/            # My study notes and summaries
└── README.md
```

Each session folder has its own `README.md` with the goal, what I built and what I learned.

## Progress

| # | Session | Topic | Status |
| --- | --- | --- | --- |
| 01 | Session 01 | Intro to Claude Code: agents, agentic loop, tools, interfaces, ecosystem, installation | ✅ Done |
| 02 | Session 02 | _TBD_ | ⏳ Pending |
| 03 | Session 03 | _TBD_ | ⏳ Pending |
| 04 | Session 04 | _TBD_ | ⏳ Pending |
| 05 | Session 05 | _TBD_ | ⏳ Pending |
| 06 | Session 06 | _TBD_ | ⏳ Pending |
| 07 | Session 07 | _TBD_ | ⏳ Pending |
| 08 | Session 08 | _TBD_ | ⏳ Pending |

## My workflow with agents

1. Write the requirement as a spec file (`docs/specs/RF-01.md`) with acceptance criteria and exceptions.
2. Ask Claude Code for a **plan** first (plan mode), and review it.
3. Turn every acceptance criterion and exception into a **test**.
4. Let the agent **implement** until all tests pass.
5. **Review** the diff myself, then **commit**.
6. Start a fresh session (`/clear`) for the next requirement.

## Getting started

```bash
# Clone the repository
git clone https://github.com/<your-username>/agentic-software-development-dotnet10.git
cd agentic-software-development-dotnet10

# Go into a session project
cd session-01-intro-to-agents

# Build and test (.NET 10 SDK required)
dotnet build
dotnet test

# Start Claude Code in the project folder
claude
```

## Author

**Evelyn**: learning to build software with AI agents, one commit at a time.

- GitHub: [@your-username](https://github.com/your-username)
- LinkedIn: [your-profile](https://linkedin.com/in/your-profile)

---

_This is a personal learning repository. Course materials belong to their respective authors; this repo contains only my own code and notes._
