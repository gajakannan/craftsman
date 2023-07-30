﻿namespace Craftsman.Builders;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Domain;
using Helpers;
using Services;

public class DatabaseEntityConfigModifier
{
    private readonly IFileSystem _fileSystem;
    private readonly IConsoleWriter _consoleWriter;

    public DatabaseEntityConfigModifier(IFileSystem fileSystem, IConsoleWriter consoleWriter)
    {
        _fileSystem = fileSystem;
        _consoleWriter = consoleWriter;
    }

    public void AddRelationships(string srcDirectory, string entityName, string entityPlural, List<EntityProperty> properties, string projectBaseName)
    {
        var classPath = ClassPathHelper.DatabaseConfigClassPath(srcDirectory, 
            $"{FileNames.GetDatabaseEntityConfigName(entityName)}.cs",
            projectBaseName);

        if (!_fileSystem.Directory.Exists(classPath.ClassDirectory))
            _fileSystem.Directory.CreateDirectory(classPath.ClassDirectory);

        if (!_fileSystem.File.Exists(classPath.FullClassPath))
        {
            _consoleWriter.WriteInfo($"The `{classPath.FullClassPath}` file could not be found.");
            return;
        }
            
        var relationshipConfigs = string.Empty;
        foreach (var entityProperty in properties.Where(x => x.Relationship == "1tomany"))
        {
            relationshipConfigs += @$"{Environment.NewLine}        builder.HasMany(x => x.{entityProperty.Name})
            .WithOne(x => x.{entityName});";
        }
        foreach (var entityProperty in properties.Where(x => x.Relationship == "1to1"))
        {
            relationshipConfigs += @$"{Environment.NewLine}        builder.HasOne(x => x.{entityProperty.Name})
            .WithOne(x => x.{entityName})
            .HasForeignKey<{entityName}>(s => s.Id);";
        }
        foreach (var entityProperty in properties.Where(x => x.Relationship == "manytomany"))
        {
            relationshipConfigs += @$"{Environment.NewLine}        builder.HasMany(x => x.{entityProperty.Name})
            .WithMany(x => x.{entityPlural});";
        }
        foreach (var entityProperty in properties.Where(x => x.Relationship == "manyto1"))
        {
            relationshipConfigs += @$"{Environment.NewLine}        builder.HasOne(x => x.{entityProperty.Name})
            .WithMany(x => x.{entityPlural});";
        }
        foreach (var entityProperty in properties.Where(x => x.Relationship == "self"))
        {
            relationshipConfigs += @$"{Environment.NewLine}        builder.HasOne(x => x.{entityProperty.Name});";
        }
        
        var tempPath = $"{classPath.FullClassPath}temp";
        using (var input = _fileSystem.File.OpenText(classPath.FullClassPath))
        {
            using var output = _fileSystem.File.CreateText(tempPath);
            {
                string line;
                while (null != (line = input.ReadLine()))
                {
                    var newText = $"{line}";
                    if (line.Contains($"Relationship Marker --"))
                    {
                        newText += relationshipConfigs;
                    }

                    output.WriteLine(newText);
                }
            }
        }

        // delete the old file and set the name of the new one to the original name
        _fileSystem.File.Delete(classPath.FullClassPath);
        _fileSystem.File.Move(tempPath, classPath.FullClassPath);
    }
}
