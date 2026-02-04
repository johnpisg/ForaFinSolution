# ForaFinSolution

## Architecture
I use a Serverless architecture, following the Clean Architecture and Some Design principles.
The solution is located in the ForaFin project, it exposes some http endpoints as Azure Functions.
It works with a in-memory database for develop mode. And use a Logs/ folder to register the logs from the app.
We need to run that project and we will get 4 endpoints urls locally.
It was build using NET8.

```dotnet clean && dotnet build && dotnet test```

## Functions

### 1. GetToken:
[GET] http://localhost:7147/api/GetToken

First we need to call this endpoint to get an access token.
Note: For develop mode purpose we are generating and validating our owns tokens.

### 2.ImportCompanies:
[POST] http://localhost:7147/api/ImportCompanies

Second we have to call this endpoint to import the companies information from the Sec's Edgar External API, import and persist that information into the database.

### 3. GetAllCompanies:
[GET] http://localhost:7147/api/GetAllCompanies

Optional we can call this endpoint to list our information stored in our database.

### 4. GetForaFinCompanies:
[POST] http://localhost:7147/api/GetForaFinCompanies

Finally, this is the main endpoint to call the main process to get the list of companies as well ad the amount of funding they are eligible to receive. It allows the user to supply a parameter 'StartsWith' as the filter inside the body of the request.


## TEST

The project ForaFinTest contains the unit testing for each azure function as well as the services.


Ing. Jhon Samamé