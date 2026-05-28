see AGENTS.md

The shared instructions now include the Windows local launcher and one-folder build script under `windows/`.
This standalone layout also includes root Windows wrappers and fork setup notes.

Quality checks for code changes:

```powershell
uv run --extra dev python -m ruff check .
uv run --extra dev python -m mypy src tests
uv run --extra dev python -m pytest tests/
```
