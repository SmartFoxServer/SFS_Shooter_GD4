# SmartFoxServer Example Projects for Godot 4.x
The series of **C# examples** built for the Godot 4 engine have been developed with **Godot Mono 4.0.3**, but although this Multiplayer Shooter example uses **Godot Mono 4.2.1**, the concepts and the code to interact with the SFS2X API are valid for any version of Godot 4.x (unless otherwise noted).

Each of the tutorials in this series examine a single example, describing its objectives, **offering an insight into the SmartFoxServer features** it wants to highlight. This project includes all the assets required to compile and test the example (both client and — if existing — server side). If necessary, code excerpts are provided in the tutorial itself (see online documentation link below), in order to better explain the approach that was followed to implement a specific feature. At the bottom of the tutorial, additional resources are linked if available.

The tutorials have an increasing complexity, from basic server connection to a complete game with authoritative server code.

Specifically, the examples in this series will showcase:

* basic connection with optional protocol encryption
* room management
* buddy list management
* game rooms and match-making
* authoritive server basics for player transforms

The Godot examples provided have been tested for exporting as native executables for Windows and macOS. At the time of writing this article (June 2023) Godot Mono does not yet support exporting for mobile platform or the browser.



# SFS_Shooter_GD4
The Shooter example shows how to develop a full multiplayer first/third person combat game with Godot 4.x and SmartFoxServer 2X. The game utilizes SmartFoxServer 2X's ability to mix TCP and UDP based messaging, and makes full use of SmartFoxServer's Lag Monitor.

TCP is the most common protocol deployed on the Internet. Its dominance is explained by the fact that TCP performs error correction. When TCP is used, there is an assurance of delivery. This is accomplished through a number of characteristics including ordered data transfer, retransmission, flow control and congestion control. During the delivery process, packet data may collide and be lost, however TCP ensures that all packet data is received by re-sending requests until the complete package is successfully delivered.

UDP is also commonly found on the Internet. However UDP is not used to deliver critical information, as it forgoes the data checking and flow control found in TCP. For this reason, UDP is significantly faster and more efficient, although, it cannot be relied on to reach its destination.

<p align="center"> 
<img width="720" alt="tictactoe" src="https://github.com/SmartFoxServer/SFS_TicTacToe_GD4/assets/30838007/423390c0-3bfd-4d03-8aa8-72ebbe66ada6">
 </p>

In this example, all critical game information between the server and client are sent via TCP for reliable delivery. This could be shooting for example, or animation sync, damage messages, etc. On the other hand, sending updates to the players Transform, which is continuously sent by and to all clients, is executed using UDP.

The example scene features a simple lobby and multiple games which can run at the same time as individual arenas. We have built this example with Godot 4.2, but it is also compatible with other 4.x versions. This example was ported from its Unity counterpart. However, the Character Transform is different than the Unity example, and although the server-side code is the same, a Unity built client cannot play the same game as a Godot built client.

## Client Code
This example is not meant to be a tutorial on how to write a 3rd person shooter in Godot. It is designed to showcase the features of SmartFoxServer, and how to utilize TCP and UDP in the same project. However, this example is extensive and could be used as the basis of a 1st or 3rd person multiplayer game and as such, we will cover the main components of the Client code, but more specifically, how they relate to the Server Extension.

The main components in the client project are found in the Scripts folder. They are as follows:

The GameManager is the main component that is attached to the Game scene. The main configurable variables are presented in the Inspector, including Player Model selection, Player Model Colors, and UI elements.
The CharacterTransform is used to position the characters in the world simulation, including character rotation, and weapon aiming. The local player sends this transform object to the server script, which in turn it is propagated to other players for synchronization.
The Player script is utilized in each of the player scenes. It controls movement, animations, for the local character, and positioning and animations are synchronized for the remote characters.

## Server Code
The Java Extension includes Handlers to communicate with the client, and Simulation code to handle the player transforms and positions within the world simulation. This means that the world simulation runs on the server side, and all clients have a representative simulation running on their system. Clients communicate with the game code on the server side via extension requests and gets data back via extension response. The data that is sent and received using SFSObjects, which are highly optimized dataobjects that can be nested. The main sections in the server code are as follows:

*The Shooter Extension main class is used to handle requests from clients and send messages back to them. All communication is done with json objects
*The Shooter Utils includes the Room Helper and User Helper, which handle the Room/Zone responses and User response respectively.
*The Shooter Handlers send and receive Objects to and from the client. These include the transform itself, as well as the spawning, shooting, and animations.
*The World Simulation includes code to control the Player transforms, spawned Items, Weapon shooting, loading, and reloading, and Game Variables.

## Setup & run
In order to setup and run the example, follow these steps:

1. unzip the examples package;
2. launch Godot, click on the Import button and navigate to the SFS_Shooter_GD4 folder
3. click the Build button in the top right corner of the Godot editor before running the example.

The client's C# code is in the Godot project's *res://scripts* folder, while the SmartFoxServer 2X client API DLLs are in the *res:// folder*.

## Server-side Extension
The server-side Extension is available in two versions: Java and JavaScript, and the game client expects the Java extension to be deployed. At the end of this article we also explain how to use the JavaScript version. Its is freely avaliable from the SmartFox Website for Download.

http://www.smartfoxserver.com/download/get/316

## Deploy the Java Extension
Copy the Shooter/ folder from SFS2X-Shooter-Ext/deploy/ to your current SFS2X installation under SFS2X/extensions/ See the Online Documentation for more information on how to install SmartFoxServer on you machine and how to include extensions.

The source code is provided under the SFS2X-Shooter-Ext/Java/src folder. You can create and setup a new project in your Java IDE of choice as described in the Writing the first Java Extension document of the Java Extensions Development section. Copy the content of the SFS2X-Shooter-Ext/Java/src folder to your Java project' source folder.

## Online Tutorial and Documentation
The base code for this example is similar to others in the series, but the Game code has been expanded to implement the new features.

The LobbyManager and GameManager classes have been updated to add the logic related to the Game Room creation and join, and the logic to send invitations. However, this code requires the server extension to be installed on the server system.

To learn more about this template and how it is configured for establishing a connection and handling SmartFoxServer events, server externsions and turn based games, go to the online documentation and tutorials linked below.

**SmartFoxServer Example Documentation**   

http://docs2x.smartfoxserver.com/ExamplesGodot/shooter

This online documentation includes:
* Client and Server Code
* Creating the Game Room
* Extension initialization
* Game Start and Player Turns
* Game over and restart
* Using the JavaScript Extension
  
 and further **Resource Links**

http://docs2x.smartfoxserver.com/ExamplesGodot/introduction

 <p align="center"> 
<img width="400" alt="connector-login" src="https://github.com/SmartFoxServer/SFS_Connector_GD4/assets/30838007/a8f025fb-5bc0-4ca6-8ce0-8ec808565303">
 </p>

