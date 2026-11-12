import argparse
import os

from aind_behavior_services.rig.harp import HarpCuttlefishfip
from aind_physiology_slap2.rig import AindPhysioSlap2Rig, Ports


def mock_rig() -> AindPhysioSlap2Rig:
    """Generates an example rig with the required fields."""
    return AindPhysioSlap2Rig(
        data_directory=r"C:/data",
        rig_name="test_rig",
        computer_name="test_computer",
        cuttlefish_fip=HarpCuttlefishfip(port_name="COM4"),
        digital_input_pin=Ports.IO0,  # 0x01
    )


def main():
    parser = argparse.ArgumentParser(description="Generate the example rig JSON file")
    parser.add_argument(
        "--path",
        default="./local/rig.json",
        help="Output path of the rig file (default: ./local/rig.json)",
    )
    args = parser.parse_args()

    os.makedirs(os.path.dirname(args.path) or ".", exist_ok=True)
    with open(args.path, "w", encoding="utf-8") as f:
        f.write(mock_rig().model_dump_json(indent=2))


if __name__ == "__main__":
    main()
