[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
![Lifecycle:Stable](https://img.shields.io/badge/Lifecycle-Stable-97ca00)

# Supreme Court Viewer

This repository contains the stand alone **Supreme Court Viewer** application.  This application allows authorized individuals to review file details and access the electronic versions of the related documentation.

## Running in Docker

Refer to [Running the Application on Docker](./docker/README.md) to details.

## Running on OpenShift

The OpenShift build and deployment configurations for the project can be found here; [bcgov/supreme-court-viewer-configurations](https://github.com/bcgov/supreme-court-viewer-configurations)

## High Level Architecture

![Court Viewer Application](./doc/diagrams/Court%20Viewer.drawio.svg)

### API Documentation

For high level View API documentation refer to the diagram above.  For details, refer to the [router](./web/src/router/index.ts) and [view components](./web/src/components/) source code.

For backend API documentation refer to the Swagger API documentation page available at the `api/` endpoint of the running application.  For example, if you are running the application locally in docker, the Swagger page can be found at https://localhost:8080/scjscv/api/.  Refer to [Running in Docker](#running-in-docker) section for details.

## Getting Help or Reporting an Issue

To report bugs/issues/feature requests, please file an [issue](../../issues).

## Deployment Process

```mermaid
flowchart TD
 subgraph Pipeline["Build on Merge"]
    direction LR
        E1(Docker Build)
        E2(Push Image to Artifactory)
        E3(Update gitops repo)
  end
 subgraph ArgoCD["Argo CD"]
    direction LR
        F1(Detect Change)
        F2(Synchronize Charts)
        F3(Deploy to OpenShift)
  end
    E1 --> E2 -- SHA tag --> E3
    F1 --> F2 --> F3
    A("Perform Code Changes")
    A --> B("Pull Request")
    B --> C("Merge PR")
    C --> E[["GitHub Actions"]]
    E --> F[["Argo CD"]]
    E -.- Pipeline
    F -.- ArgoCD
     E:::gitActionStyle
     F:::argoStlye
     Pipeline:::gitActionStyle
     ArgoCD:::argoStlye
    classDef default fill: #FFF
    classDef gitActionStyle stroke: #AA00FF,fill: #FFF
    classDef argoStlye stroke: #00FF88,fill: #FFF
```

## How to Contribute

If you would like to contribute, please see our [CONTRIBUTING](./CONTRIBUTING.md) guidelines.

Please note that this project is released with a [Contributor Code of Conduct](./CODE_OF_CONDUCT.md).
By participating in this project you agree to abide by its terms.
