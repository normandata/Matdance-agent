# Skills

Language: English | [中文](zh-CN/skills.md)

Skills are reusable local procedures. They are not a generic knowledge dump.

## What A Skill Should Contain

A good skill explains:

- when to use it;
- when not to use it;
- prerequisites;
- concrete steps;
- required files, scripts, templates, or assets;
- validation method;
- common failure modes.

## Organization

The skill organizer can extract reusable procedures from completed work. It should only record facts and operations that were actually practiced or verified. It must not create skills from vague promises, uncertain guesses, or imagined future plans.

Skill organization may create skill-local helper assets when those assets are based on collapsed facts and are needed for the skill to work. This avoids fake references to nonexistent files.

The background organizer uses adaptive batching. It starts with a larger pending-message batch and a small `skill_read` window, then reduces the batch size or read window when provider context limits or repeated structured-output failures are hit. Successful runs recover toward the default size over time.

During organization, `skill_read` is round-based: each round can expose only a small window of candidate skill manuals, and the subagent must immediately mark every read skill as related or unrelated. Unrelated manuals are discarded before the next round. Existing skills can be updated, deleted, or superseded only if they were retained as related. Raw tool results from the source conversation are not injected into skill extraction context; tool call names and arguments are kept because they are the reusable procedure.

If the same evidence range fails more than twice after downgrade attempts, the organizer skips that poisoned batch and continues with later evidence. This may miss a potential skill, but it prevents one bad timeline range from blocking future skill discovery.

## Validation

Skill validation checks whether a skill can be used independently and safely. Validation reports can mark skills as valid, needs changes, invalid, or risky.

If a report is `needs_changes` or `invalid`, the skill can re-enter the idle validation queue after repair. This lets skills converge over time instead of getting stuck after one bad validation result.

## Export And Import

Skill export packages the skill directory into a zip file. Everything inside the skill goes into the package.

Import still uses learn-and-validate. External skill packages are treated as untrusted material and localized before they become part of the agent's skill library.

## Boundaries

Skills must not modify Matdance source code, runtime state, credentials, cookie stores, or unrelated workspace files. Validation and repair should stay inside the skill-local scope unless a system tool explicitly allows more.
