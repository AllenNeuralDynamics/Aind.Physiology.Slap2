from enum import IntFlag
from typing import Literal

from aind_behavior_services import rig
from aind_behavior_services.rig import harp
from pydantic import Field

from . import __semver__


class Ports(IntFlag):
    """Available hardware ports in the cuttlefish board."""

    NONE = 0
    IO0 = 1 << 0
    IO1 = 1 << 1
    IO2 = 1 << 2
    IO3 = 1 << 3
    IO4 = 1 << 4
    IO5 = 1 << 5
    IO6 = 1 << 6
    IO7 = 1 << 7


class AindPhysioSlap2Rig(rig.Rig):
    """Rig configuration model for the AIND SLAP2 physiology system."""

    version: Literal[__semver__] = __semver__
    cuttlefish_fip: harp.HarpCuttlefishfip = Field(
        title="CuttlefishFip",
        description="CuttlefishFip board for controlling the trigger of cameras and light-sources",
    )
    digital_input_pin: Ports = Field(description="Cuttlefish port whose digital input is monitored.")
