
<div align="center">
    <img src="/res/AutoUnlaunch.png" height="200" width="200" />
	<h1>AutoUnlaunch</h1>
	<p>
		<i>Automatically stop game launchers when you're done gaming.</i>
	</p>
    <a href="https://apps.microsoft.com/detail/AutoUnlaunch/9NZ2KJ2H6V6L?mode=direct">
        <img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
    </a>
</div>

## Introduction
AutoUnlaunch is an open source app for Windows that will automatically take care of stopping game launchers when you're done playing a game. It's intended to minimize how often you have to interact with game launchers and get you back to doing what you really want – play games. It supports multiple popular game launchers and each is individually configurable.

## Background
So I don't like game launchers. I just want to be able to launch a game from an icon or shortcut and never see a game launcher. Unfortunately, game launchers are unavoidable these days. So to make it more tolerable, I wrote this app so I can launch a game, play it, and not have to worry about stopping the launcher that started alongside when I launched the game. It even include options and tips to keep game launcher out of sight when possible. Now I can simply launch a game, play a game, and exit a game when I'm done (for the most part).

## Features
- Supports Steam, EA, GOG Galaxy, and Epic Games launchers.
- Configurable delay before stopping a launcher.
- Multiple methods to stop a launcher including a graceful request to stop.
- Additional tips for each launcher.

> **Note:** While most games for a supported launcher is expected to work, some may not because not every game can be tested. Please open a GitHub issue or pull request to improve compatibility.

## Basic Instructions
1. Configure the settings for each game launcher that's supported. Disable the ones that aren't relevant to you.
2. Close AutoUnlaunch and, if necessary, allow it to run in the background.
3. Start playing some games! When you exit a game, the game's launcher will be stopped automatically based on the settings you've configured.

## Building
### Prerequisites
- Visual Studio 2022
  - .NET desktop development workload
  - Universal Windows Platform development workload
  - Windows 11 SDK (10.0.22621.0)
- .NET 8 SDK

### Build and Run
1. Open the [`AutoUnlaunch.sln`](/AutoUnlaunch.sln) solution.
2. If not already set, set the `AutoUnlaunch` project as the startup project.
3. Select a build configuration (Debug or Release) and architecture (x64, x86, or ARM64).
4. Start debugging.

### Execute Tests
The tests can be executed in Visual Studio's Test Explorer window. Additionally, there is a [`runTests.ps1`](/scripts/runTests.ps1) script that will execute all of the tests and generate a code coverage report. This requires the `reportgenerator` dotnet global tool to be installed.
```shell
dotnet tool install -g dotnet-reportgenerator-globaltool
```
