using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using System.Text.RegularExpressions;
using Unity.Physics.Authoring;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;


public class MyTool : EditorWindow
{
    [MenuItem("My Tool/Update PhysicsCategoryNames")]
    public static void ShowWindow()
    {
        Object obj = Selection.activeObject; //Get the object/

        if (obj == null) //Check for the null.
            return;
        else
            Debug.Log("Please select a PhysicsCategoryNames!");

        if (obj.GetType() == typeof(PhysicsCategoryNames))
        {
            PhysicsCategoryNames physicsCategoryNames = obj as PhysicsCategoryNames; //Here is your type casted object.

            // Generate enum code
            SyntaxTree syntaxTree = GenerateEnumSyntaxTree(physicsCategoryNames);

            // Save generated code to file
            string filePath = Path.Combine(Application.dataPath, $"_GENERATED/PhysicsCategory.cs");
            using (StreamWriter stream = new StreamWriter(filePath))
            {
                syntaxTree.GetRoot().NormalizeWhitespace().WriteTo(stream);
            }
            CompilationPipeline.RequestScriptCompilation();
            AssetDatabase.Refresh();
        }
        else
            Debug.Log("Please select a PhysicsCategoryNames!");
    }

    private static SyntaxTree GenerateEnumSyntaxTree(PhysicsCategoryNames physicsCategoryNames)
    {
        // Create enum declaration
        ClassDeclarationSyntax classDeclaration = SyntaxFactory.ClassDeclaration("PhysicsCategory")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));

        for (int i = 0; i < physicsCategoryNames.CategoryNames.Count; i++)
        {
            string name = (physicsCategoryNames.CategoryNames[i]);
            if (name != null && name != string.Empty)
            {
                name = SanitizeName(physicsCategoryNames.CategoryNames[i]); ;
                classDeclaration = classDeclaration.AddMembers(GenerateFieldDeclaration(name, $"1u << {i}"));
            }
        }

        // Create the namespace and add the class to it
        NamespaceDeclarationSyntax @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("_GENERATED"))
            .AddMembers(classDeclaration);

        // Create the compilation unit (the root of the tree) and add the namespace to it
        CompilationUnitSyntax compilationUnit = SyntaxFactory.CompilationUnit()
            .AddMembers(@namespace)
            .NormalizeWhitespace();

        // Create syntax tree
        return SyntaxFactory.SyntaxTree(compilationUnit);
    }
    public static FieldDeclarationSyntax GenerateFieldDeclaration(string name, string initializer)
    {
        return SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntKeyword))
            )
            .AddVariables(
                SyntaxFactory.VariableDeclarator(name)
                .WithInitializer(
                    SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.ParseExpression(initializer)
                    )
                )
            )
        )
        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                      SyntaxFactory.Token(SyntaxKind.ConstKeyword));
    }

    private static string SanitizeName(string name)
    {
        // Replace invalid characters with underscores
        name = Regex.Replace(name, @"[^A-Za-z0-9_]", "_");

        // Ensure the name starts with a letter or underscore
        if (!char.IsLetter(name[0]) && name[0] != '_')
        {
            name = "_" + name;
        }

        return name;
    }
}
