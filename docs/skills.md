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

## Validation

Skill validation checks whether a skill can be used independently and safely. Validation reports can mark skills as valid, needs changes, invalid, or risky.

If a report is `needs_changes` or `invalid`, the skill can re-enter the idle validation queue after repair. This lets skills converge over time instead of getting stuck after one bad validation result.

## Export And Import

Skill export packages the skill directory into a zip file. Everything inside the skill goes into the package.

Import still uses learn-and-validate. External skill packages are treated as untrusted material and localized before they become part of the agent's skill library.

## Boundaries

Skills must not modify Matdance source code, runtime state, credentials, cookie stores, or unrelated workspace files. Validation and repair should stay inside the skill-local scope unless a system tool explicitly allows more.
