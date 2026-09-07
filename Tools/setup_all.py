"""Execute in Unreal Editor to import both the full-size campus and builder kit."""
from pathlib import Path
import runpy
import unreal

root=Path(unreal.Paths.project_dir())
runpy.run_path(str(root/"Tools"/"setup_unreal.py"),run_name="__main__")
runpy.run_path(str(root/"Tools"/"setup_walkthrough.py"),run_name="__main__")
