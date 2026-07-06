{
  description = "STS2_YGO dev shell";
  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  outputs = { self, nixpkgs }:
  let
    system = "x86_64-linux";
    pkgs = nixpkgs.legacyPackages.${system};
  in {
    devShells.${system}.default = pkgs.mkShell {
      buildInputs = [
        pkgs.gh
        pkgs.jq
        pkgs.powershell
        pkgs.dotnet-sdk_9
        pkgs.godotPackages_4_5.godot-mono
      ];

      shellHook = ''
        export DOTNET_ROOT=${pkgs.dotnet-sdk_9}/share/dotnet
        echo "已进入 STS2_YGO 开发环境"
      '';
    };
  };
}