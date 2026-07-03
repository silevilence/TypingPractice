# Engine

This directory contains the typing state machine, comparison flow, and the baseline statistics provider used by the core session implementation.

## Input Flow

- Raw key events from the front end should arrive with `ImeCommitText = null`. These events contribute physical keystroke counts and composition tracking only.
- Text that is actually committed to the article should arrive through `ImeCommitText`, regardless of whether it came from IME commit or a direct text input path.
- A raw backspace while there is still uncommitted composition input is treated as IME-internal editing and does not remove committed characters.
- A raw backspace after committed text has advanced the session removes the last committed character and reopens that text position for retyping.