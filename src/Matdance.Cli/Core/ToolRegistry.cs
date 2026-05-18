using System.Text.Json;
using System.Text.Json.Nodes;
using Matdance.Cli.Services;

namespace Matdance.Cli.Core;

public static class ToolRegistry
{
    public static List<ToolDefinition> GetAll(bool includeScheduledTaskTools = true)
    {
        var tools = new List<ToolDefinition>
        {
            Bash(),
            TaskManager(),
            MemoryStore(),
            MemorySearch(),
            FileSearch(),
            FileTraceOpen(),
            FileTraceShow(),
            FileTraceClose(),
            FileRead(),
            FileWrite(),
            FileWriteLocks(),
            FileWriteLockClose()
        };

        if (includeScheduledTaskTools)
        {
            tools.AddRange(new[]
            {
                ScheduledTaskCreate(),
                ScheduledTaskEdit(),
                ScheduledTaskList(),
                ScheduledTaskRead(),
                ScheduledTaskDo(),
                ScheduledTaskDelete()
            });
        }

        tools.AddRange(new[] { SkillCreate(), SkillRead(), SkillEditor(), SkillDelete() });
        tools.Add(ImageGenerationListProfiles());
        tools.Add(ImageGeneration());
        tools.Add(ImageGenerationShowProcess());
        tools.Add(ImageGenerationCancel());
        tools.Add(ImageGenerationRetry());
        tools.Add(TextToSpeechListProfiles());
        tools.Add(TextToSpeech());
        tools.Add(WebSearchListProfiles());
        tools.Add(WebSearch());
        tools.AddRange(BrowserTools());
        return tools;
    }

    private static ToolDefinition Bash()
    {
        var privacyState = PrivacyToolDescription();
        return new ToolDefinition
        {
            Name = "bash",
            Description = $"Execute a shell command through {MatdanceRuntime.ShellInvocation} on {MatdanceRuntime.OsName}. Use commands and path syntax appropriate for this OS. When installing or downloading packages/assets, choose per-command/project-scoped download sources appropriate to the user's inferred region and the downloader involved, such as pip indexes, npm/pnpm/yarn registries, conda channels, Maven/Gradle repositories, NuGet feeds, Go proxies, Rust registries, OS package mirrors, model hubs, or vendor release mirrors. Dangerous commands require user confirmation. The command runs from the agent workspace and has a bounded timeout; foreground dev servers, watchers, and long-running processes will be terminated on timeout, including child processes. Use short bounded checks instead of leaving servers running inside this tool. The command must not access Matdance source/internal state, credentials, cookie stores, parent directories, or private user paths unless Settings explicitly allows privacy access. {privacyState} If privacy access is disabled, do not call this tool to probe user-private paths, home/profile folders, cloud drives, account content, or known-folder/environment-variable shortcuts.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["command"] = new JsonObject { ["type"] = "string", ["description"] = $"The command to execute via {MatdanceRuntime.ShellInvocation}" },
                    ["timeout"] = new JsonObject { ["type"] = "integer", ["description"] = "Timeout in seconds, clamped to 1-120. Defaults to 30; likely foreground servers/watchers default to a shorter bounded check.", ["default"] = 30 }
                },
                ["required"] = new JsonArray("command")
            }
        };
    }

    private static ToolDefinition TaskManager()
    {
        return new ToolDefinition
        {
            Name = "task_manager",
            Description = "Manage the current session's active task. Only one task can be in_process at a time.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["action"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray("create", "update", "done", "status"),
                        ["description"] = "Action to perform"
                    },
                    ["title"] = new JsonObject { ["type"] = "string", ["description"] = "Task title (for create)" },
                    ["steps"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["description"] = "List of step descriptions (for create). Use at most 3 steps; Matdance may compact or drop extra steps to keep the UI stable.",
                        ["items"] = new JsonObject { ["type"] = "string" }
                    },
                    ["step_index"] = new JsonObject { ["type"] = "integer", ["description"] = "Step index to update (for update)" },
                    ["status"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("in_process", "done", "failed", "skipped"), ["description"] = "New status (for update)" },
                    ["note"] = new JsonObject { ["type"] = "string", ["description"] = "Update note (for update)" }
                },
                ["required"] = new JsonArray("action")
            }
        };
    }

    private static ToolDefinition MemoryStore()
    {
        return new ToolDefinition
        {
            Name = "memory_store",
            Description = "Append a narrative memory entry into hot_memory or core_memory. Never overwrites previous memory; use hot for concise recent working context and core for durable requirements, reusable preferences, lessons, and future-useful facts. Scheduled organization later compacts old hot entries into long-term memory.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["target"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray("hot", "core"),
                        ["description"] = "Target memory: hot (recent narrative context) or core (durable requirements, preferences, lessons, reusable facts)"
                    },
                    ["content"] = new JsonObject { ["type"] = "string", ["description"] = "Narrative memory entry to append; do not include the entire old memory" }
                },
                ["required"] = new JsonArray("target", "content")
            }
        };
    }

    private static ToolDefinition MemorySearch()
    {
        return new ToolDefinition
        {
            Name = "memory_search",
            Description = "Search memory by exact long-term date or by local vector index. Keyword queries use deterministic local embeddings plus rerank and return relevant snippets.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["query"] = new JsonObject { ["type"] = "string", ["description"] = "Search keywords" },
                    ["date"] = new JsonObject { ["type"] = "string", ["description"] = "Exact date YYYY-MM-DD (optional)" }
                },
                ["required"] = new JsonArray("query")
            }
        };
    }

    private static ToolDefinition FileRead()
    {
        var privacyState = PrivacyToolDescription();
        return new ToolDefinition
        {
            Name = "file_read",
            Description = $"Read a full text-compatible file and keep a compatibility read trace. Do not use this for raster/image visual inspection: image files are visual inputs, not binary/code text, and the tool will refuse to dump them. Prefer `file_search` plus `file_trace_open/show/close` for debugging and edits because trace locks expose live windows and force you to manage context. Use untrace=true to stop tracking a file. By default this is limited to the agent workspace and preview-safe runtime output; private user files require the global privacy access switch. {privacyState} Matdance source, runtime state, credentials, cookie stores, task run records, and secret-bearing files are never readable through this tool.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Relative or absolute file path" },
                    ["untrace"] = new JsonObject { ["type"] = "boolean", ["description"] = "If true, remove file from trace list instead of reading", ["default"] = false },
                    ["limit"] = new JsonObject { ["type"] = "integer", ["description"] = "Max characters to read (default 50000)", ["default"] = 50000 }
                },
                ["required"] = new JsonArray("path")
            }
        };
    }

    private static ToolDefinition FileWrite()
    {
        return new ToolDefinition
        {
            Name = "file_write",
            Description = "Write, append, or replace expected text in a workspace file. Successful writes automatically open or refresh a write lock around the changed region so you can verify the current file reality. For targeted edits, prefer expected+replace_with instead of rewriting a whole file. This tool cannot modify Matdance source, plugin source, runtime state, credentials, scheduled run records, cookie stores, agent config, or private user locations.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Relative or absolute file path" },
                    ["content"] = new JsonObject { ["type"] = "string", ["description"] = "Content to write or append. Required for overwrite/append mode." },
                    ["append"] = new JsonObject { ["type"] = "boolean", ["description"] = "Append content instead of overwriting", ["default"] = false },
                    ["expected"] = new JsonObject { ["type"] = "string", ["description"] = "Exact current text to replace. Use with replace_with for safer targeted edits." },
                    ["replace_with"] = new JsonObject { ["type"] = "string", ["description"] = "Replacement text for expected." },
                    ["replace_all"] = new JsonObject { ["type"] = "boolean", ["description"] = "Replace every occurrence of expected instead of the first occurrence.", ["default"] = false }
                },
                ["required"] = new JsonArray("path")
            }
        };
    }

    private static ToolDefinition FileSearch()
    {
        var privacyState = PrivacyToolDescription();
        return new ToolDefinition
        {
            Name = "file_search",
            Description = $"Search files for navigation only. Results may include line numbers and snippets, but they are not stable edit coordinates; open a read trace before relying on nearby content. {privacyState}",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["query"] = new JsonObject { ["type"] = "string", ["description"] = "Single literal or regex query" },
                    ["queries"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" }, ["description"] = "Batch queries" },
                    ["paths"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" }, ["description"] = "Files or directories to search. Omit to search the workspace projects directory." },
                    ["regex"] = new JsonObject { ["type"] = "boolean", ["default"] = false },
                    ["case_sensitive"] = new JsonObject { ["type"] = "boolean", ["default"] = false },
                    ["max_matches"] = new JsonObject { ["type"] = "integer", ["default"] = 80 },
                    ["before"] = new JsonObject { ["type"] = "integer", ["default"] = 1 },
                    ["after"] = new JsonObject { ["type"] = "integer", ["default"] = 1 }
                }
            }
        };
    }

    private static ToolDefinition FileTraceOpen()
    {
        return new ToolDefinition
        {
            Name = "file_trace_open",
            Description = "Open one or more turn-scoped read locks for text-compatible files. Do not use read locks for raster/image visual inspection; image files are visual inputs, not binary/code text. Read locks are live windows over current files, max 3 total, max 2000 lines per lock, with file-size, metadata-read, and text-read timeout guards. The host clears all read/write locks when the reply turn finishes. Use semantic mode with anchor text for code blocks that may move; use physical mode for scanning a fixed line range. Close locks you no longer need during the turn.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Single file path" },
                    ["locks"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["path"] = new JsonObject { ["type"] = "string" },
                                ["anchor"] = new JsonObject { ["type"] = "string", ["description"] = "Text to semantically anchor around" },
                                ["mode"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("semantic", "physical", "full"), ["default"] = "semantic" },
                                ["start_line"] = new JsonObject { ["type"] = "integer" },
                                ["end_line"] = new JsonObject { ["type"] = "integer" },
                                ["max_lines"] = new JsonObject { ["type"] = "integer", ["default"] = 240, ["description"] = "1-2000" }
                            },
                            ["required"] = new JsonArray("path")
                        }
                    },
                    ["anchor"] = new JsonObject { ["type"] = "string" },
                    ["mode"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("semantic", "physical", "full") },
                    ["start_line"] = new JsonObject { ["type"] = "integer" },
                    ["end_line"] = new JsonObject { ["type"] = "integer" },
                    ["max_lines"] = new JsonObject { ["type"] = "integer", ["default"] = 240 }
                }
            }
        };
    }

    private static ToolDefinition FileTraceShow()
    {
        return new ToolDefinition
        {
            Name = "file_trace_show",
            Description = "Refresh and show current live content from this turn's read locks and/or write locks. The live lock output is more authoritative than your memory or older tool results. Output and refresh work are bounded; if a lock reports metadata_timeout/read_timeout, close it or open a narrower/local trace. Locks are cleared when the reply turn finishes.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["ids"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" }, ["description"] = "Specific read or write lock ids. Omit to show all locks." },
                    ["kind"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("read", "write", "all"), ["default"] = "all" }
                }
            }
        };
    }

    private static ToolDefinition FileTraceClose()
    {
        return new ToolDefinition
        {
            Name = "file_trace_close",
            Description = "Close read locks or write locks that are no longer useful. Free locks before opening distant windows.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["ids"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" } },
                    ["kind"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("read", "write", "all"), ["default"] = "read" },
                    ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Optional path; closes locks for this file and kind." }
                }
            }
        };
    }

    private static ToolDefinition FileWriteLocks()
    {
        return new ToolDefinition
        {
            Name = "file_write_locks",
            Description = "List or refresh current write locks. Use after writes to verify the changed region is correct, then close locks that are no longer needed. Output and refresh work are bounded; timeout locks must not be used for edits.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["ids"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" } }
                }
            }
        };
    }

    private static ToolDefinition FileWriteLockClose()
    {
        return new ToolDefinition
        {
            Name = "file_write_lock_close",
            Description = "Close one or more write locks after you have verified the modified region.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["ids"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" } },
                    ["path"] = new JsonObject { ["type"] = "string" }
                }
            }
        };
    }
    private static ToolDefinition ScheduledTaskCreate() => ScheduledTool("scheduled_task_create", "Create a low-priority scheduled task for the current agent. Supports once, interval, after_count, daily, daily_count, daily_window and daily_times.", new JsonArray("title", "content", "schedule"));
    private static ToolDefinition ScheduledTaskEdit() => ScheduledTool("scheduled_task_edit", "Edit an existing scheduled task when the user explicitly asks to change, pause, resume, retarget, or reschedule it. Do not use this as an automatic repair action after a failure; system maintenance handles repair/retry fallbacks separately.", new JsonArray("task_id"));
    private static ToolDefinition ScheduledTaskList() => ScheduledTool("scheduled_task_list", "List scheduled tasks with pagination.", new JsonArray());
    private static ToolDefinition ScheduledTaskRead() => ScheduledTool("scheduled_task_read", "Read one scheduled task and recent execution history.", new JsonArray("task_id"));
    private static ToolDefinition ScheduledTaskDo() => ScheduledTool("scheduled_task_do", "Run a scheduled task once for explicit testing; avoid unless user asks.", new JsonArray("task_id"));
    private static ToolDefinition ScheduledTaskDelete() => ScheduledTool("scheduled_task_delete", "Soft-delete a scheduled task and keep history. Use only when the user explicitly asks to remove or stop the task permanently.", new JsonArray("task_id"));

    private static ToolDefinition ScheduledTool(string name, string description, JsonArray required)
    {
        return new ToolDefinition
        {
            Name = name,
            Description = description,
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["task_id"] = new JsonObject { ["type"] = "string" },
                    ["title"] = new JsonObject { ["type"] = "string" },
                    ["content"] = new JsonObject { ["type"] = "string" },
                    ["timezone"] = new JsonObject { ["type"] = "string", ["description"] = "IANA timezone id, e.g. Asia/Shanghai. Defaults to system local timezone." },
                    ["status"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("enabled", "paused"), ["description"] = "Task status" },
                    ["schedule"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["description"] = "Schedule configuration. MUST include a 'type' field. Examples: {type:'daily',time:'09:30'}, {type:'daily_times',times:['09:00','14:00']}, {type:'once',runAt:'2026-05-08T09:30:00'}, {type:'daily_window',windowStart:'09:00',windowEnd:'18:00',intervalMinutes:60}",
                        ["properties"] = new JsonObject
                        {
                            ["type"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("once", "interval", "after_count", "daily", "daily_count", "daily_window", "daily_times"), ["description"] = "Required schedule type" },
                            ["runAt"] = new JsonObject { ["type"] = "string", ["description"] = "ISO datetime for 'once' type, e.g. 2026-05-08T09:30:00" },
                            ["time"] = new JsonObject { ["type"] = "string", ["description"] = "Time string HH:mm for 'daily' type, e.g. 09:30" },
                            ["times"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" }, ["description"] = "Array of HH:mm strings for 'daily_times', e.g. ['09:00','14:00']" },
                            ["windowStart"] = new JsonObject { ["type"] = "string", ["description"] = "Window start HH:mm for 'daily_window'" },
                            ["windowEnd"] = new JsonObject { ["type"] = "string", ["description"] = "Window end HH:mm for 'daily_window'" },
                            ["intervalMinutes"] = new JsonObject { ["type"] = "integer", ["description"] = "Interval in minutes for interval/daily_window/daily_count" },
                            ["startAt"] = new JsonObject { ["type"] = "string", ["description"] = "ISO start datetime for 'interval'/'after_count'" },
                            ["endAt"] = new JsonObject { ["type"] = "string", ["description"] = "ISO end datetime for 'interval'/'after_count'" },
                            ["maxRuns"] = new JsonObject { ["type"] = "integer", ["description"] = "Maximum runs for 'after_count'" },
                            ["countPerDay"] = new JsonObject { ["type"] = "integer", ["description"] = "Executions per day for 'daily_count'" },
                            ["startTime"] = new JsonObject { ["type"] = "string", ["description"] = "Start HH:mm for 'daily_count'" },
                            ["startDate"] = new JsonObject { ["type"] = "string", ["description"] = "Start date YYYY-MM-DD" },
                            ["endDate"] = new JsonObject { ["type"] = "string", ["description"] = "End date YYYY-MM-DD" }
                        },
                        ["required"] = new JsonArray("type")
                    },
                    ["targets"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["type"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("created_session", "session", "all_agent_sessions") },
                                ["sessionId"] = new JsonObject { ["type"] = "string", ["description"] = "Required when type is 'session'." }
                            },
                            ["required"] = new JsonArray("type")
                        },
                        ["description"] = "Delivery targets. Each item: {type:'created_session'|'session'|'all_agent_sessions', sessionId?}. Omit to deliver to created session."
                    },
                    ["page"] = new JsonObject { ["type"] = "integer" },
                    ["page_size"] = new JsonObject { ["type"] = "integer" },
                    ["deliver"] = new JsonObject { ["type"] = "boolean", ["description"] = "For scheduled_task_do only: whether to deliver result to target sessions." }
                },
                ["required"] = required
            }
        };
    }

    private static ToolDefinition SkillCreate()
    {
        return new ToolDefinition
        {
            Name = "skill_create",
            Description = "Create a new skill for practiced, confirmed, reusable workflows, domain best practices, or vertical expertise. Skills persist across sessions and help maintain consistency. Content should be reproducible and include when to use it, preconditions, workflow, tools/parameters, expected outputs, failure handling, and boundaries. Do not create skills from guesses, promises, future plans, ordinary chat summaries, private-data access patterns, credential handling, or unverified commands/configurations.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["name"] = new JsonObject { ["type"] = "string", ["description"] = "Skill name (max 50 chars)" },
                    ["description"] = new JsonObject { ["type"] = "string", ["description"] = "Brief description of what this skill covers (max 300 chars)" },
                    ["tags"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" }, ["description"] = "Optional tags for categorization" },
                    ["content"] = new JsonObject { ["type"] = "string", ["description"] = "Full reproducible skill content. Include sections for When to Use, Preconditions, Workflow, Tools and Parameters, Expected Outputs, Failure Handling, and Boundaries when applicable." }
                },
                ["required"] = new JsonArray("name", "description", "content")
            }
        };
    }

    private static ToolDefinition SkillRead()
    {
        return new ToolDefinition
        {
            Name = "skill_read",
            Description = "Read a skill's full content plus current validation/import report notes before starting a relevant task or maintaining the skill.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["skill_id"] = new JsonObject { ["type"] = "string", ["description"] = "The skill ID from the Skills section" }
                },
                ["required"] = new JsonArray("skill_id")
            }
        };
    }

    private static ToolDefinition SkillEditor()
    {
        return new ToolDefinition
        {
            Name = "skill_editor",
            Description = "Edit an existing skill to update its content, description, or tags after completing a workflow or discovering confirmed improvements. Keep content reproducible: when to use it, preconditions, workflow, tools/parameters, expected outputs, failure handling, and boundaries. Do not turn guesses, promises, future plans, ordinary chat summaries, private-data access patterns, credential handling, or unverified commands/configurations into durable skill instructions.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["skill_id"] = new JsonObject { ["type"] = "string", ["description"] = "The skill ID to edit" },
                    ["name"] = new JsonObject { ["type"] = "string", ["description"] = "New name (optional)" },
                    ["description"] = new JsonObject { ["type"] = "string", ["description"] = "New description (optional)" },
                    ["tags"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" }, ["description"] = "New tags (optional)" },
                    ["content"] = new JsonObject { ["type"] = "string", ["description"] = "New full content (optional)" }
                },
                ["required"] = new JsonArray("skill_id")
            }
        };
    }

    private static ToolDefinition SkillDelete()
    {
        return new ToolDefinition
        {
            Name = "skill_delete",
            Description = "Delete a skill permanently. Use sparingly.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["skill_id"] = new JsonObject { ["type"] = "string", ["description"] = "The skill ID to delete" }
                },
                ["required"] = new JsonArray("skill_id")
            }
        };
    }

    private static ToolDefinition ImageGeneration()
    {
        return new ToolDefinition
        {
            Name = "image_generation",
            Description = "Start an asynchronous host-managed image generation job through the configured image model profiles and save results into the agent workspace. The host job state and image_generation_show_process are the only authoritative sources for status, provider fallback, errors, and file locations. Keep prompt concise: normally 1-30 characters; only use 31-50 characters when the user explicitly asks for a complex scene or the scene cannot be shortened. Omit profile to use the configured default/auto profile order. Use the same batch_id for related images. Do not wait synchronously for completion; continue other work and query progress when needed.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["profile"] = new JsonObject { ["type"] = "string", ["description"] = "Optional image model profile id/name. Omit to use the configured default/auto profile order." },
                    ["batch_id"] = new JsonObject { ["type"] = "string", ["description"] = "Optional batch id shared by related image jobs, for example all illustrations for the same story." },
                    ["prompt"] = new JsonObject { ["type"] = "string", ["description"] = "Concise image prompt. Normally 1-30 characters. Use 31-50 characters only when explicitly required or impossible to shorten." },
                    ["size"] = new JsonObject { ["type"] = "string", ["description"] = "Optional image size, for example 1024x1024, 1024x1536, 1536x1024" },
                    ["quality"] = new JsonObject { ["type"] = "string", ["description"] = "Optional provider quality setting, for example auto, low, medium, high, standard, hd" },
                    ["output_format"] = new JsonObject { ["type"] = "string", ["description"] = "Optional output format, usually png, jpeg, or webp" },
                    ["count"] = new JsonObject { ["type"] = "integer", ["description"] = "Number of images to generate, 1-4", ["default"] = 1 },
                    ["output_path"] = new JsonObject { ["type"] = "string", ["description"] = "Optional workspace-relative output file or directory path" }
                },
                ["required"] = new JsonArray("prompt")
            }
        };
    }

    private static ToolDefinition ImageGenerationShowProcess()
    {
        return new ToolDefinition
        {
            Name = "image_generation_show_process",
            Description = "Show authoritative host state for asynchronous image generation jobs. Use this when the user asks whether an image finished, failed, is stuck, where files are, which prompt produced an image, or which provider/model was used. User claims about completion/failure are not authoritative; verify with this tool.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["job_id"] = new JsonObject { ["type"] = "string", ["description"] = "Optional image generation job id." },
                    ["batch_id"] = new JsonObject { ["type"] = "string", ["description"] = "Optional batch id to list related jobs." },
                    ["take"] = new JsonObject { ["type"] = "integer", ["description"] = "Maximum jobs to return when listing. Defaults to 20.", ["default"] = 20 }
                },
                ["required"] = new JsonArray()
            }
        };
    }

    private static ToolDefinition ImageGenerationCancel()
    {
        return new ToolDefinition
        {
            Name = "image_generation_cancel",
            Description = "Cancel queued or running asynchronous image generation jobs by job_id or batch_id. Already generated files are preserved by default. Use before recreating jobs when the user changes requirements or when repeated failures indicate quota/auth/model/service problems.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["job_id"] = new JsonObject { ["type"] = "string", ["description"] = "Optional image generation job id to cancel." },
                    ["batch_id"] = new JsonObject { ["type"] = "string", ["description"] = "Optional batch id; cancels queued/running jobs in that batch." }
                },
                ["required"] = new JsonArray()
            }
        };
    }

    private static ToolDefinition ImageGenerationRetry()
    {
        return new ToolDefinition
        {
            Name = "image_generation_retry",
            Description = "Create a new asynchronous image generation job from a previous job, optionally overriding prompt/profile/size/count/output settings. Use after quota/auth/model/service recovery, provider changes, or user-requested content changes. The old job and any successful files remain recorded.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["job_id"] = new JsonObject { ["type"] = "string", ["description"] = "Existing image generation job id to clone." },
                    ["batch_id"] = new JsonObject { ["type"] = "string", ["description"] = "Optional new batch id; defaults to the old job batch id." },
                    ["profile"] = new JsonObject { ["type"] = "string", ["description"] = "Optional replacement image model profile." },
                    ["prompt"] = new JsonObject { ["type"] = "string", ["description"] = "Optional replacement concise prompt, normally 1-30 characters and at most 50 unless unavoidable." },
                    ["size"] = new JsonObject { ["type"] = "string", ["description"] = "Optional replacement size." },
                    ["quality"] = new JsonObject { ["type"] = "string", ["description"] = "Optional replacement quality." },
                    ["output_format"] = new JsonObject { ["type"] = "string", ["description"] = "Optional replacement output format." },
                    ["count"] = new JsonObject { ["type"] = "integer", ["description"] = "Optional replacement count, 1-4." },
                    ["output_path"] = new JsonObject { ["type"] = "string", ["description"] = "Optional replacement output path." }
                },
                ["required"] = new JsonArray("job_id")
            }
        };
    }

    private static ToolDefinition ImageGenerationListProfiles()
    {
        return new ToolDefinition
        {
            Name = "image_generation_list_profiles",
            Description = "List configured image generation profiles/providers for the current agent, including the default/auto profile order, model, endpoint, size, quality, output format, enabled state, and whether an API key is configured. Call this before image_generation when the user asks about available providers or when provider choice matters.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject(),
                ["required"] = new JsonArray()
            }
        };
    }

    private static ToolDefinition TextToSpeech()
    {
        return new ToolDefinition
        {
            Name = "text_to_speech",
            Description = "Generate speech audio files through configured TTS profiles and save them into the agent workspace. This is usually not something to call proactively for ordinary chat. Use it when the user asks for a specific sentence, line, script, narration, or voice asset; also use it when it is reasonably part of the task, for example creating narration for a video edit or producing voice assets for creative work. Long narration, scripts, chapters, and verbose prose should be batched into sentence-bounded chunks when you control the tool calls. The host may also retry retryable long-input/payload/timeout failures by splitting into up to 10 sentence-ended chunks and merging one final audio file. Omit profile to use the configured default/auto profile order. If provider or voice choice matters, call text_to_speech_list_profiles first. After generation succeeds, show the generated audio with {show_file:PATH} unless the user asks for paths only.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["profile"] = new JsonObject { ["type"] = "string", ["description"] = "Optional TTS profile id/name/voice/model. Omit to use the configured default/auto profile order." },
                    ["text"] = new JsonObject { ["type"] = "string", ["description"] = "Text to synthesize. Use this for a single audio asset." },
                    ["texts"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["description"] = "Optional list of texts to synthesize into separate audio assets. Use either text or texts. Prefer this for intentionally separate lines/assets; if the user needs one long final audio, keep each text sentence-bounded and let the host fallback merge only when needed.",
                        ["items"] = new JsonObject { ["type"] = "string" }
                    },
                    ["voice"] = new JsonObject { ["type"] = "string", ["description"] = "Optional voice override supported by the selected provider/profile." },
                    ["format"] = new JsonObject { ["type"] = "string", ["description"] = "Optional output format, usually mp3, wav, webm, opus, m4a, aac, or flac." },
                    ["output_path"] = new JsonObject { ["type"] = "string", ["description"] = "Optional workspace-relative output file or directory path." },
                    ["allow_profile_fallback"] = new JsonObject { ["type"] = "boolean", ["description"] = "If true, fall back to other enabled TTS profiles when the requested profile fails. Defaults to true only when profile is omitted." }
                },
                ["required"] = new JsonArray()
            }
        };
    }

    private static ToolDefinition TextToSpeechListProfiles()
    {
        return new ToolDefinition
        {
            Name = "text_to_speech_list_profiles",
            Description = "List configured text-to-speech profiles/providers for the current agent, including the default/auto profile order, model, endpoint, voice, format, mode, enabled state, auto-play behavior, and whether an API key is configured. Call this before text_to_speech when the user asks about available voices/providers or when provider choice matters.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject(),
                ["required"] = new JsonArray()
            }
        };
    }

    private static ToolDefinition WebSearch()
    {
        return new ToolDefinition
        {
            Name = "web_search",
            Description = "Search the web through configured search provider profiles such as Tavily, Brave Search, or Firecrawl. Use when the user asks for current information, source discovery, or web research and the provider profile is enabled. Omit profile to use enabled profiles in configured order with fallback. Do not use search to bypass paywalls, authentication, CAPTCHA, or robots/terms restrictions.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["query"] = new JsonObject { ["type"] = "string", ["description"] = "Search query." },
                    ["profile"] = new JsonObject { ["type"] = "string", ["description"] = "Optional search profile id/name/provider. Omit to use the configured default/auto profile order." },
                    ["max_results"] = new JsonObject { ["type"] = "integer", ["description"] = "Maximum results to return, clamped to 1-20. Defaults to the profile setting.", ["default"] = 5 },
                    ["allow_profile_fallback"] = new JsonObject { ["type"] = "boolean", ["description"] = "If true, fall back to other enabled search profiles when the requested profile fails. Defaults to true only when profile is omitted." }
                },
                ["required"] = new JsonArray("query")
            }
        };
    }

    private static ToolDefinition WebSearchListProfiles()
    {
        return new ToolDefinition
        {
            Name = "web_search_list_profiles",
            Description = "List configured web search profiles/providers for the current agent, including provider, endpoint, enabled state, default result limit, and whether an API key is configured. Call this before web_search when provider choice matters or search is unavailable.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject(),
                ["required"] = new JsonArray()
            }
        };
    }

    private static List<ToolDefinition> BrowserTools()
    {
        var privacyState = PrivacyToolDescription();
        return new List<ToolDefinition>
        {
            new ToolDefinition
            {
                Name = "browser_navigate",
                Description = $"Navigate the isolated Matdance-controlled background browser to a URL. This cannot control or read the user's normal browser profiles, tabs, history, extensions, cookies, or passwords. Launches the controlled browser automatically if not running; visible/headless=false launch requests are ignored so automation does not pull a foreground browser over the user. Preserve the current task's browser state: do not navigate away, refresh, or switch headless/visible mode as a generic recovery tactic. If the site requires login, account selection, verification, or CAPTCHA, stop and ask the user to complete it through an available user-controlled auth surface; do not bypass or dismiss authentication walls. {privacyState} If privacy access is disabled, do not navigate to file:// paths or private/account pages for the purpose of reading user-private content.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["url"] = new JsonObject { ["type"] = "string", ["description"] = "URL to navigate to" },
                        ["headless"] = new JsonObject { ["type"] = "boolean", ["description"] = "Compatibility option only. Matdance keeps browser automation in background mode; headless=false is ignored.", ["default"] = true },
                        ["wait_network_idle"] = new JsonObject { ["type"] = "integer", ["description"] = "Seconds to wait for network idle after navigation, clamped to 0-30. Default 0 = don't wait.", ["default"] = 0 }
                    },
                    ["required"] = new JsonArray("url")
                }
            },
            new ToolDefinition
            {
                Name = "browser_click",
                Description = "Click an element on the current page by CSS selector. Do not click controls merely to close, hide, or bypass login/authentication prompts; ask the user to log in when authentication is required.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["selector"] = new JsonObject { ["type"] = "string", ["description"] = "CSS selector of the element to click" },
                        ["timeout"] = new JsonObject { ["type"] = "integer", ["description"] = "Timeout in milliseconds, clamped to 500-30000. Defaults to 5000.", ["default"] = 5000 }
                    },
                    ["required"] = new JsonArray("selector")
                }
            },
            new ToolDefinition
            {
                Name = "browser_type",
                Description = "Type text into an input field by CSS selector.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["selector"] = new JsonObject { ["type"] = "string", ["description"] = "CSS selector of the input field" },
                        ["text"] = new JsonObject { ["type"] = "string", ["description"] = "Text to type" },
                        ["submit"] = new JsonObject { ["type"] = "boolean", ["description"] = "Press Enter after typing (default false)", ["default"] = false },
                        ["timeout"] = new JsonObject { ["type"] = "integer", ["description"] = "Timeout in milliseconds, clamped to 500-30000. Defaults to 5000.", ["default"] = 5000 }
                    },
                    ["required"] = new JsonArray("selector", "text")
                }
            },
            new ToolDefinition
            {
                Name = "browser_screenshot",
                Description = "Take a screenshot of the current page. Returns the file path.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["output_path"] = new JsonObject { ["type"] = "string", ["description"] = "File path to save screenshot (optional, defaults to temp dir)" },
                        ["full_page"] = new JsonObject { ["type"] = "boolean", ["description"] = "Capture full page instead of viewport (default false)", ["default"] = false }
                    },
                    ["required"] = new JsonArray()
                }
            },
            new ToolDefinition
            {
                Name = "browser_get_content",
                Description = $"Get the text content or HTML of the current page. {privacyState} If privacy access is disabled, do not use this to read private/account pages, mailboxes, chats, cloud-drive files, or other authenticated user-private content.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["html"] = new JsonObject { ["type"] = "boolean", ["description"] = "Return HTML instead of text (default false)", ["default"] = false },
                        ["max_length"] = new JsonObject { ["type"] = "integer", ["description"] = "Max characters to return, clamped to 500-50000. Defaults to 12000.", ["default"] = 12000 }
                    },
                    ["required"] = new JsonArray()
                }
            },
            new ToolDefinition
            {
                Name = "browser_evaluate",
                Description = $"Execute short JavaScript in the current page context and return the result. This tool has a bounded timeout; do not wait inside the script for navigation, network idle, login, timers, or long-running UI conditions. Do not use JavaScript to bypass login, hide authentication overlays, defeat paywalls, extract credential material, cookies, localStorage/sessionStorage tokens, or access content the user has not authenticated for. {privacyState} If privacy access is disabled, do not use JavaScript to extract private/account content.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["script"] = new JsonObject { ["type"] = "string", ["description"] = "Short JavaScript code to execute. Prefer synchronous DOM reads/mutations and return quickly." },
                        ["timeout"] = new JsonObject { ["type"] = "integer", ["description"] = "Timeout in milliseconds, clamped to 1000-30000. Defaults to 8000.", ["default"] = 8000 }
                    },
                    ["required"] = new JsonArray("script")
                }
            },
            new ToolDefinition
            {
                Name = "browser_wait_for",
                Description = $"Wait for bounded dynamic-page readiness on the current page: CSS selector, visible text, URL substring/regex, load state, or a short read-only JavaScript predicate. Use this instead of loops in browser_evaluate. This is not for bypassing login, CAPTCHA, anti-bot, paywall, or account checks; ask the user to authenticate when required. {privacyState}",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["kind"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("selector", "text", "url", "load_state", "function"), ["description"] = "Wait condition type." },
                        ["selector"] = new JsonObject { ["type"] = "string", ["description"] = "CSS selector for kind=selector." },
                        ["text"] = new JsonObject { ["type"] = "string", ["description"] = "Text fragment or regex body for kind=text/url/function." },
                        ["state"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("attached", "detached", "visible", "hidden", "domcontentloaded", "load", "networkidle"), ["description"] = "Selector state or load state. Defaults to visible for selectors and networkidle for load_state." },
                        ["regex"] = new JsonObject { ["type"] = "boolean", ["description"] = "Treat text as a regular expression for text/url waits.", ["default"] = false },
                        ["timeout"] = new JsonObject { ["type"] = "integer", ["description"] = "Timeout in milliseconds, clamped to 500-30000. Defaults to 10000.", ["default"] = 10000 }
                    },
                    ["required"] = new JsonArray("kind")
                }
            },
            new ToolDefinition
            {
                Name = "browser_query",
                Description = $"Return structured, bounded DOM candidates from the current page, including tag, text preview, role/label/name, href, visibility, and selector hints. Use this to inspect dynamic pages before clicking. It intentionally omits cookie/localStorage/sessionStorage values and must not be used to extract secrets or private account content unless privacy access explicitly allows the task. {privacyState}",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["selector"] = new JsonObject { ["type"] = "string", ["description"] = "Optional CSS selector. Defaults to common interactive/content elements." },
                        ["text"] = new JsonObject { ["type"] = "string", ["description"] = "Optional case-insensitive text filter." },
                        ["limit"] = new JsonObject { ["type"] = "integer", ["description"] = "Maximum elements, clamped to 1-100. Defaults to 30.", ["default"] = 30 }
                    },
                    ["required"] = new JsonArray()
                }
            },
            new ToolDefinition
            {
                Name = "browser_source_analyze",
                Description = $"Return a structured source-level summary of the current page: script/style inventory, forms, metadata, links, and inline handler locations. Use this for page architecture/source analysis before resorting to custom JavaScript. It does not read cookies, localStorage, sessionStorage, indexedDB, or credential values. Inline source previews are disabled by default and should be enabled only when the task needs code inspection and privacy rules allow it. {privacyState}",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["include_inline"] = new JsonObject { ["type"] = "boolean", ["description"] = "Include short previews of inline script/style/handler source. Defaults to false.", ["default"] = false },
                        ["limit"] = new JsonObject { ["type"] = "integer", ["description"] = "Maximum items per category, clamped to 1-200. Defaults to 80.", ["default"] = 80 }
                    },
                    ["required"] = new JsonArray()
                }
            },
            new ToolDefinition
            {
                Name = "browser_verify",
                Description = $"Verify a bounded page condition and return ok/failed diagnostics without long custom JavaScript: selector state, visible text, URL condition, load state, or a short safe predicate. Use this after navigation, clicks, typing, injection, or crawl steps to confirm behavior. It is not a bypass for login, CAPTCHA, paywalls, or account checks. {privacyState}",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["kind"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("selector", "text", "url", "load_state", "function"), ["description"] = "Verification condition type." },
                        ["selector"] = new JsonObject { ["type"] = "string", ["description"] = "CSS selector for kind=selector." },
                        ["text"] = new JsonObject { ["type"] = "string", ["description"] = "Text fragment, URL fragment/regex body, or short JavaScript predicate for kind=function." },
                        ["state"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("attached", "detached", "visible", "hidden", "domcontentloaded", "load", "networkidle"), ["description"] = "Selector state or load state." },
                        ["regex"] = new JsonObject { ["type"] = "boolean", ["description"] = "Treat text as regex for text/url verification.", ["default"] = false },
                        ["negate"] = new JsonObject { ["type"] = "boolean", ["description"] = "Verify the condition is absent/false where supported.", ["default"] = false },
                        ["timeout"] = new JsonObject { ["type"] = "integer", ["description"] = "Timeout in milliseconds, clamped to 500-30000. Defaults to 10000.", ["default"] = 10000 }
                    },
                    ["required"] = new JsonArray("kind")
                }
            },
            new ToolDefinition
            {
                Name = "browser_crawl",
                Description = $"Perform a bounded link crawl from the current page or start_url. It only follows discovered http(s) links, defaults to same-origin, returns title/text previews/link summaries, redacts sensitive URL query values, and has a 90-second total tool budget. It does not click buttons/forms, bypass login, or read browser storage. {privacyState} If privacy access is disabled, do not crawl private/account pages to extract user-private content.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["start_url"] = new JsonObject { ["type"] = "string", ["description"] = "Optional starting URL. Defaults to the current page if it is http(s)." },
                        ["max_pages"] = new JsonObject { ["type"] = "integer", ["description"] = "Maximum pages, clamped to 1-20. Defaults to 5.", ["default"] = 5 },
                        ["max_depth"] = new JsonObject { ["type"] = "integer", ["description"] = "Maximum link depth, clamped to 0-3. Defaults to 1.", ["default"] = 1 },
                        ["same_origin"] = new JsonObject { ["type"] = "boolean", ["description"] = "Only follow same-origin links. Defaults to true.", ["default"] = true },
                        ["max_chars"] = new JsonObject { ["type"] = "integer", ["description"] = "Maximum text preview characters per page, clamped to 200-8000. Defaults to 2000.", ["default"] = 2000 },
                        ["restore"] = new JsonObject { ["type"] = "boolean", ["description"] = "Navigate back to the original page after crawling when possible. Defaults to true.", ["default"] = true }
                    },
                    ["required"] = new JsonArray()
                }
            },
            new ToolDefinition
            {
                Name = "browser_trace",
                Description = "Start, read, or stop a bounded browser event trace for current/future page activity. It records high-level network request/response URLs/status/resource types and console messages only; it does not record request/response headers, bodies, cookies, storage, credentials, or token values. Sensitive URL query values are redacted.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["action"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("start", "read", "stop"), ["description"] = "Trace action. Defaults to read.", ["default"] = "read" },
                        ["network"] = new JsonObject { ["type"] = "boolean", ["description"] = "Record high-level network events when starting. Defaults to true.", ["default"] = true },
                        ["console"] = new JsonObject { ["type"] = "boolean", ["description"] = "Record console events when starting. Defaults to true.", ["default"] = true },
                        ["max_events"] = new JsonObject { ["type"] = "integer", ["description"] = "Trace ring buffer size, clamped to 20-1000. Defaults to 200.", ["default"] = 200 },
                        ["take"] = new JsonObject { ["type"] = "integer", ["description"] = "Events to return for read/stop, clamped to 1-300. Defaults to 80.", ["default"] = 80 }
                    },
                    ["required"] = new JsonArray()
                }
            },
            new ToolDefinition
            {
                Name = "browser_scroll",
                Description = "Scroll the current page or an element in bounded steps with a 45-second total tool budget, optionally stopping when a selector or text appears. Use for lazy-loaded pages and infinite-scroll result lists. Do not use it to evade access controls or anti-bot challenges.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["selector"] = new JsonObject { ["type"] = "string", ["description"] = "Optional scroll container selector. Omit for the page viewport." },
                        ["direction"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("down", "up", "left", "right"), ["description"] = "Scroll direction.", ["default"] = "down" },
                        ["pixels"] = new JsonObject { ["type"] = "integer", ["description"] = "Pixels per step, clamped to 100-3000. Defaults to 900.", ["default"] = 900 },
                        ["steps"] = new JsonObject { ["type"] = "integer", ["description"] = "Maximum scroll steps, clamped to 1-30. Defaults to 1.", ["default"] = 1 },
                        ["until_selector"] = new JsonObject { ["type"] = "string", ["description"] = "Optional selector to stop on when visible." },
                        ["until_text"] = new JsonObject { ["type"] = "string", ["description"] = "Optional text fragment to stop on when visible in body text." },
                        ["delay"] = new JsonObject { ["type"] = "integer", ["description"] = "Delay between steps in milliseconds, clamped to 0-3000. Defaults to 300.", ["default"] = 300 }
                    },
                    ["required"] = new JsonArray()
                }
            },
            new ToolDefinition
            {
                Name = "browser_inject_init_script",
                Description = "Install a JavaScript init script for future navigations in the controlled browser context, for lightweight instrumentation such as marking loaded components, collecting non-sensitive DOM timing, or tracing high-level network events. This tool rejects scripts that mention cookies, storage, credentials, passwords, tokens, CAPTCHA, paywalls, webdriver/navigator spoofing, privileged request headers, service workers, or access-control/anti-bot bypass patterns.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["script"] = new JsonObject { ["type"] = "string", ["description"] = "Init script to run before future page scripts. Max 25000 characters." },
                        ["purpose"] = new JsonObject { ["type"] = "string", ["description"] = "Short human-readable reason for the injection." }
                    },
                    ["required"] = new JsonArray("script", "purpose")
                }
            },
            new ToolDefinition
            {
                Name = "save_cookie",
                Description = $"Save the current controlled-browser context cookies for the current agent. Defaults to saving all cookies; optional site filters by registrable site such as example.com. This controlled browser-state operation is not disabled by the privacy access switch, but cookie values must never be displayed, exported, summarized, or handed to users, scripts, third parties, or uncontrolled environments. {privacyState}",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["site"] = new JsonObject { ["type"] = "string", ["description"] = "Optional site/domain/URL filter. Omit to save all browser cookies." }
                    },
                    ["required"] = new JsonArray()
                }
            },
            new ToolDefinition
            {
                Name = "list_cookie_by_site",
                Description = "List saved browser cookies grouped by registrable site, folding subdomains such as mail.example.com under example.com. This controlled browser-state operation is not disabled by the privacy access switch. Cookie values are intentionally not returned and must not be requested through any other tool.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["site"] = new JsonObject { ["type"] = "string", ["description"] = "Optional site/domain/URL filter. Omit to list all saved cookie sites." }
                    },
                    ["required"] = new JsonArray()
                }
            },
            new ToolDefinition
            {
                Name = "apply_cookie",
                Description = $"Apply saved browser cookies to the current controlled-browser context. Defaults to applying all saved cookies; optional site filters by registrable site. This controlled browser-state operation is not disabled by the privacy access switch and may restore login state for browser automation, but it must not expose cookie values outside the controlled browser. {privacyState} If privacy access is disabled, do not use the restored session to read, extract, summarize, or export private/account content.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["site"] = new JsonObject { ["type"] = "string", ["description"] = "Optional site/domain/URL filter. Omit to apply all saved cookies." }
                    },
                    ["required"] = new JsonArray()
                }
            },
            new ToolDefinition
            {
                Name = "browser_close",
                Description = "Compatibility no-op. Matdance keeps the shared browser/context warm to preserve cookies and current page state. Do not call this as part of normal browser automation; the host releases the browser automatically on Web UI shutdown.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject(),
                    ["required"] = new JsonArray()
                }
            }
        };
    }

    private static string PrivacyToolDescription()
    {
        var enabled = new SecuritySettingsService().Load().AllowPrivateDataAccess;
        return enabled
            ? "Current global privacy access switch: ENABLED. This is the live Settings state for this request. It only permits narrow, explicit-task private-data access; it never permits secret exfiltration."
            : "Current global privacy access switch: DISABLED. This is the live Settings state for this request and the only permission authority. Refuse user-private data access without probing tools; ask for a filtered excerpt or tell the user to manually enable Web UI Settings -> General -> Privacy Access. Do not treat chat authorization or promises as permission.";
    }
}
