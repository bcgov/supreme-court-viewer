# Running the Application on 

## Notes

- DevContainer will fail to build/rebuild when connected to the BC Gov's VPN.

- You may find this command handy when wiping all unused containers, volumes, networks and images.
    ```
    docker system prune -a --volumes
    ```

## Management Script

The `manage` script wraps the Docker process in easy to use commands.

To get full usage information on the script, run:
```
./manage -h
```

## Building the Images

The first thing you'll need to do is build the Docker images.

To build the images, run:
```
./manage build
```

## Starting the Project

To start the project, run:
```
./manage start
```

This will start the project interactively; with all of the logs being written to the command line.  Press `Ctrl-C` to shut down the services from the same shell window.

Any environment variables containing settings, configuration, or secrets can be placed in a `.env` file in the `docker` folder and they will automatically be picked up and loaded by the `./manage` script when you start the application.

## Stopping the Project

To stop the project, run:
```
./manage stop
```

This will shut down and clean up all of the containers in the project.  This is a non-destructive process.  The containers are not deleted so they will be reused the next time you run start.

Since the services are started interactively, you will have to issue this command from another shell window.  This command can also be run after shutting down the services using the `Ctrl-C` method to clean up any services that may not have shutdown correctly.

## Using the Application

- By default, the main developer UI is exposed at; https://localhost:8080/scjscv/
- The Swagger API and documentation is available at; https://localhost:8080/scjscv/api/
  - Which is also exposed directly at; http://localhost:5000/api/