import typing as t
from pathlib import Path

import pydantic
from aind_behavior_services.schema import BonsaiSgenSerializers, convert_pydantic_to_bonsai
from aind_behavior_services.session import Session

SCHEMA_ROOT = Path("./schema/")
EXTENSIONS_ROOT = Path("./src/Extensions/")
NAMESPACE_PREFIX = "AindPhysiologySlap2"


def main():
    """Regenerate the JSON schema and the Bonsai C# classes from the pydantic models."""
    models = [
        Session,
    ]

    # Wrapping in a root model makes every model a named definition in the schema.
    model = pydantic.RootModel[t.Union[tuple(models)]]  # noqa: UP007
    if len(models) == 1:
        # A single-model union collapses into the model itself; keep it as a named definition.
        model = pydantic.RootModel[list[models[0]]]

    convert_pydantic_to_bonsai(
        model,
        model_name="aind_physiology_slap2",
        root_element="Root",
        cs_namespace=NAMESPACE_PREFIX,
        json_schema_output_dir=SCHEMA_ROOT,
        cs_output_dir=EXTENSIONS_ROOT,
        cs_serializer=[BonsaiSgenSerializers.JSON],
    )


if __name__ == "__main__":
    main()
