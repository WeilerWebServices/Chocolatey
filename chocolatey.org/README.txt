## Chocolatey.org

http://chocolatey.org

## CONTRIBUTING

### Getting started
 1. Before starting, you will need:
    1. Node.js and npm installed globally
    1. A local running instand of SQL Server and Visual Studio 2013 or above
 1. In **Visual Studio**, open **ChocolateyGallery.sln**
 1. In the **Website** project, open **web.config**
 1. Locate the **connectionStrings** section, and modify the **NuGetGallery** connection string to point to your local instance of SQL Server. It's not a bad idea to change the database name (initial catalog) just to avoid confusion. For example if your SQL Server instance is named SQL2016, you could use:
`    <add name="NuGetGallery" connectionString="Data Source=.\SQL2016;Initial Catalog=Chocolatey;Integrated Security=SSPI;MultipleActiveResultSets=False;" providerName="System.Data.SqlClient" />`
*Note: the database does not need to exist*
 1. Open the Package Manager Console (**Tools > NuGet Package Manager > Package Manager Console**)
 1. If prompted, restore any missing NuGet packages (Don't use `nuget restore` from the command-line as this will pull in extra packages that cause the build to break)
 1. Run the **Update-Database** command in Package Manager Console (if the command is not found, try reloading the solution in Visual Studio)
 1. The database named in the connection string should now exist in your SQL Server.
 1. Press **Ctrl-F5** to start the web application without debugging
 1. Your web browser should launch, showing the chocolatey home page
 1. Click **Signup** and enter details to create a new account
 1. To make this account an administrator, run the following SQL against your new database eg. Use [**SQL Server Management Studio**](https://chocolatey.org/packages/sql-server-management-studio) or **sqlcmd** from the command line.
*Note: Replace 'username' with the name of the user account you just created*
```
DECLARE @adminId int
SELECT @adminId = [Key] FROM Roles WHERE Name = 'Admins'

DECLARE @userId int
SELECT @userId = [Key] FROM Users where Username = 'username'

INSERT INTO UserRoles(UserKey, RoleKey) VALUES(@userId, @adminId)
```
 13. You should now be up and running!

### Known issues

When debugging, you might see this exception: `SimpleInjector.ActivationException: 'The given type IControllerActivator is not a concrete type. Please use one of the other overloads to register this type.'`. 
Just ignore this and keep running.

### Leave items as they are in the nugetgallery folder

 1. Please don't fix anything in the `nugetgallery` folder or subfolder. Instead you should copy the unchanged item over to `chocolatey/Website`.
 1. Then remove the old item from the Visual Studio project and add the newly copied item to the project.
 1. Save the project and commit your changes.
 1. Make your changes as normal now.
