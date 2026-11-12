from typing import Literal

from aind_behavior_services import rig
from aind_behavior_services.rig import harp
from pydantic import Field

from . import __semver__


class Slap2Cuttlefish(harp.HarpCuttlefish):
    """Cuttlefish board configuration for the SLAP2 system."""
    digital_input_pin: int = Field(default=0x01, title="Digital Input Pin", description="Digital input pin for the SLAP2 cuttlefish board")

class AindPhysioSlap2Rig(rig.Rig):
    """Rig configuration model for the AIND SLAP2 physiology system."""

    version: Literal[__semver__] = __semver__
    cuttlefish_fip: Slap2Cuttlefish = Field(
        title="CuttlefishFip",
        description="CuttlefishFip board for controlling the trigger of cameras and light-sources",
    )
