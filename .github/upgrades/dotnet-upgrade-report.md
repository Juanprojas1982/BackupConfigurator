# .NET 9.0 Upgrade Report

## Project target framework modifications

| Project name                           | Old Target Framework | New Target Framework | Commits  |
|:---------------------------------------|:--------------------:|:--------------------:|----------|
| BackupConfigurator.Core.csproj         | net8.0               | net9.0               | 13677c3d |
| BackupConfigurator.UI.csproj           | net8.0-windows       | net9.0-windows       | c6e4c5ae |

## All commits

| Commit ID | Description                                                                                                                                                                                                          |
|:----------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 2e20bee1  | Commit upgrade plan                                                                                                                                                                                                  |
| 13677c3d  | Update BackupConfigurator.Core.csproj to target .NET 9.0 - Changed the target framework in BackupConfigurator.Core.csproj from net8.0 to net9.0 to upgrade the project to the latest .NET version.                 |
| c6e4c5ae  | Update BackupConfigurator.UI.csproj to target .NET 9.0 - Changed the target framework in BackupConfigurator.UI.csproj from net8.0-windows to net9.0-windows to upgrade the project to .NET 9.0.                     |

## Next steps

- Test your application thoroughly to ensure all functionality works as expected with .NET 9.0
- Review any deprecation warnings and update your code accordingly
- Consider updating your CI/CD pipelines to use .NET 9.0 SDK
- Review the official .NET 9.0 migration guide for any breaking changes that may affect your application