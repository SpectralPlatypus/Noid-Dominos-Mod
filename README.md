# Noid Domino's Mod

This is a mod for [Yo! Noid 2: Game of a Year Edition](noid.pizza) that integrates Domino's Pizza API into the game via a new NPC. Now you can order pizza from the comfort of your home, without having to tab out of your favorite video game!

![noid_pizza0](https://github.com/SpectralPlatypus/Noid-Dominos-Mod/assets/50896763/0349dc34-d5ba-4e62-9bdf-790ec506b6af)

## Usage
This mod requires Pepperoni Mod API for YN2, available [here](https://github.com/SpectralPlatypus/Pepperoni/releases/tag/v3.5).
- Extract the contents of the release package under `%NoidGameInstallPath%/noid_data/Managed/Mods`
- Talk to the NPC "Birb" underneath the pizza tower (you must have cleared at least one level for this NPC to appear in the void)
- He will take you through the steps to complete your pizza order in no time!
![noid_pizza](https://github.com/SpectralPlatypus/Noid-Dominos-Mod/assets/50896763/211f5a31-4ec8-4d8e-8f81-df51822646df)
![noid_pizza3](https://github.com/SpectralPlatypus/Noid-Dominos-Mod/assets/50896763/9f6a9e96-0699-4718-a686-610898e29ff5)

## Projects Used
- [UnityLitJson](https://github.com/Mervill/UnityLitJson/) - the only JSON parser that I could get to work seamlessly with .NET 3.5/Unity 2017.4
- [DominosSharp](https://github.com/FromDarkHell/DominoSharp) - heavily gutted to replace Newtonsoft with UnityLitJson
