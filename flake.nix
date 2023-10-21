{
  description = "MacroMate";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
    dalamud-distrib-repo = {
      url = "github:goatcorp/dalamud-distrib";
      flake = false;
    };
  };

  outputs = { self, nixpkgs, flake-utils, dalamud-distrib-repo }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs {
          inherit system;
        };

        mkShell = pkgs.mkShell.override {
          stdenv = pkgs.clangStdenv;
        };

        dotnet = pkgs.dotnet-sdk_7;

        # Once nix flakes support zip files with top-level folders we can remove this and just point
        # the flake straight at the zip file.
        dalamud-distrib = pkgs.runCommand "dalamud-distrib" { buildInputs = [ pkgs.unzip ]; } ''
          unzip ${dalamud-distrib-repo}/latest.zip -d $out
        '';
      in {
        devShell = mkShell {
          buildInputs = [
            pkgs.omnisharp-roslyn
            dotnet
          ];

          DOTNET_ROOT=dotnet;
          DALAMUD_HOME="${dalamud-distrib}";
        };
      }
    );
}
