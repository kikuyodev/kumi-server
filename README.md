<p align="center">
	<!-- TODO: move this to the kumi server -->
	<img src="https://cdn.discordapp.com/attachments/234057206784458754/1183983961232392192/logo_coloured.png" />
	<p align="center">
	     An over exhaustive server that acts as the underlying central brain behind all things related to <a href="(https://github.com/kikuyodev/kumi">Kumi</a>
    </p>
    <hr />
</p>

Welcome to the repository of Kumi's own Server! This is primarily where all the magic happens, such as the processing of important data, and the handling of
everything related to open communications between the game, and everything surrounding it. If you're looking for the database handling and the underlying
REST API that handles the display, and distribution of data, then you'll find that [here](https://github.com/kikuyodev/kumi-api).

# State
This project is currently in an eternally very early stage of development, and is not ready for use. If you're interested in contributing, then feel free to do so! A lot of areas are still in need of work, and any help is appreciated. Additionally, usage of this server requires a properly set up database, which is
available over at the [kumi-api](https://github.com/kikuyodev/kumi-api) repository.

## Requirements
* .NET Core 6.0, or higher;
* A proper [PostgreSQL](https://www.postgresql.org/) database;
* A running [Redis](https://redis.io/) instance;
* An IDE of your choice, such as [Visual Studio](https://visualstudio.microsoft.com/), or [Rider](https://www.jetbrains.com/rider/).

### Setup

Everything is pretty straightforward, and should be easy to set up. The only thing that you'll need to do is to set up the database connection string, which is
done through the `App.config` file. An example of the file is provided in the server directory.

# Contributing

If you're interested in contributing, then feel free to do so! Any help is appreciated, and you can contribute in any way you want. If you're looking for a place to start, then you can check out the [issues](https://github.com/kikuyodev/kumi-server/issues) page, and see if there's anything that you can help with. If you're looking to add a new feature, then feel free to do so as well! Just make sure to open an issue first, so that we can discuss it.

However, as this is a server for a game, all features must be implemented in a way that is compatible with the game. All packets and their surrounding data must be published within the game's code, unless it's a necessary model, or Redis queue item.

## Support

Support for this project is provided and handled over GitHub. If you have any questions, or issues, then feel free to open an issue, and we'll get back to you as soon as possible.