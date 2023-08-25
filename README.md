# UCM Agents for the Geometry Friends game

This repository contains the code both used and developed during the production of the Undergraduate Final Project *Development of Artificial Intelligence techniques
for the dynamic and collaborative environment Geometry Friends*.

## Repository structure:

This repository contains the following folders:

- **Other Agents**: Contains agents from previous competitions that have been directly downloaded from the [Geometry Friends submissions webpage](https://geometryfriends.gaips.inesc-id.pt/archive). It is divided in as many folders as agents we have used for our comparisons, except for the "Not Used Agents" folder, which contains all other previous agents whose performance was too low to be interesting to compare.
- **Definitive Agents**: Contains the latest versions of our agents for the three game modes (only circle agent, only rectangle agent or cooperative mode). Each of these agents is located in its corresponding folder, which also contain the corresponding reports. Further information about each agent is available in the corresponding folders.
- **Final Project Agents (TFG)**: Contains the last version of our agents before handing our Final Undergraduate Project for the Complutense University of Madrid. Also contains the memory we submitted, which has detailed information of the implementation decisions and process. These agents were later modified to participate in the competition.
- **Previous Versions**: Contains past, incomplete, versions that we developed during the course of the project. This is just for version comparison and is irrelevant for most users.
- **Results**: Contains files with the results obtained by each agent or human during our testing process. It is divided into three folders corresponding to the three game modes, having each one four different competitions in which we compared our latest agents.
- **Additional Material**: Contains videos with the best achieved times for most of the rectangle levels from the competitions we used, to show how the use of unintentional game mechanics are necessary to achieve the highest scores. It also contains the Q-tables we have obtained as a result from our trainings during the development of our agents.

## How to run an agent

To try one of these agents, there are several methods to run the project:

- If you are using Windows, you can just execute the GeometryFriends.exe file inside GeometryFriendsGame/Release. Once on the main menu, select the "Agents only" option and choose the level you want to test the agent on. Further information about how to execute each agent is available in the corresponding folders, especially to change between UCM Physics and UCM QLearning circle agents.
- If you are using Linux/Mac or if you want to configure something (for example, change from the UCM Physics agent to the UCM QLearning agent when testing the circle agent), you need to follow the steps shown in this [tutotrial](https://geometryfriends.gaips.inesc-id.pt/guides/c%23). This steps are also summarized in the **Definitive Agents** folder.
