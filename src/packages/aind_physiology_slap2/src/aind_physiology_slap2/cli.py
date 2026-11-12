import typing as t

from pydantic import Field, RootModel
from pydantic_settings import BaseSettings, CliApp, CliSubCommand

from aind_physiology_slap2 import __semver__, regenerate


class VersionCli(RootModel):
    root: t.Any

    def cli_cmd(self) -> None:
        print(__semver__)


class DslRegenerateCli(RootModel):
    root: t.Any

    def cli_cmd(self) -> None:
        regenerate.main()


class Slap2Cli(BaseSettings, cli_prog_name="slap2", cli_kebab_case=True):
    version: CliSubCommand[VersionCli] = Field(
        description="Print the version of the aind-physiology-slap2 package.",
    )
    regenerate: CliSubCommand[DslRegenerateCli] = Field(
        description="Regenerate the aind-physiology-slap2 dsl dependencies.",
    )

    def cli_cmd(self):
        return CliApp().run_subcommand(self)


def main():
    CliApp().run(Slap2Cli)


if __name__ == "__main__":
    main()
