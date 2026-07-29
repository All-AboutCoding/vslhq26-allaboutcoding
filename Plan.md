Help Obe-one-Copilot.

We are trying to create a proof of concept game and have a time limit of 4 hours.
Here is the goal of our game:


This game will run in the command line interface (CLI) and will be a simple text-based adventure game. 
The player will navigate through a series of 3 enemies, each with its own description and set of abilities. 
At the beginning of the game, the program must prompt the player to enter their name and type of character (e.g., Warrior, Mage, Archer). 
Each character type will have different strengths and weaknesses.
Then the program must prommpt the player to choose the theme of the game (e.g., Fantasy, Sci-Fi, Horror).
Based on user input, have an LLM generate a unique storyline and set of 3 enemies for the player to face that follow the selected theme. 
The game will consist of a series of 3 encounters with enemies, where the player will have to make choices that affect the outcome of the game.

The game will end if the player defeats all 3 enemies or if the player's health reaches zero.
The player's health will be carried over from one enemy to another and not reset.
At the end of the game, use a AI image generator to create a unique image based on the player's character and the theme of the game. 
This image will be displayed and be able to be downloaded by the player's machine as a reward for completing the game.
The image should be in a 4k 16x9 format.
The overall tone of the image (e.g., dark, whimsical, epic) should match the outcome of the game's events, 
such as if the player won (bright), or lost (gloomy).
Allow the user to start over from the beginning or exit the game after the player has lost or won.

Here are additional requirements:
1. The game should be implemented in .NET 10 (C#) and run in the command line interface (CLI).
2. Need to implement a simple combat system where the player can choose to attack, defend, or use a special ability based on their character type.
3. The program must be able to access a LLM to generate the storyline and enemies dynamically based on the player's choices, 
	but does not have any subscriptions to any models. Please use a free to use model, or possibly download a local model.
4. All code should have easy to read comments to explain the logic and flow of the game.
5. Keep the coding style simple, and avoid using complex design patterns or advanced programming concepts.

