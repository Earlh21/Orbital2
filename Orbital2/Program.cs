using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using Orbital2.Game.Astrobodies;
using Orbital2.Physics;
using Orbital2.Physics.Collision;
using Orbital2.Physics.Gravity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using Orbital2;
using Orbital2.Engine;
using Orbital2.Game;
using Orbital2.Game.Utility;
using Orbital2.ML;
using Orbital2.ML.Schema;
using Orbital2.ML.Training;

using var game = new Orbital();

const int numPlanets = 10000;

const float maxDistance = 10000;
const float minDistance = 200;
const float maxMass = 20;
const float minMass = 3;

const float starMass = 200000;

//GameSetup.GenerateSimpleSetup(0, 200, 10, 20, 0, 5)

void GenerateTrainingData(Func<TrainingSolver.TrainingSetup> setupGenerator, int numSetups, int batchSize, string path)
{
    var trainingData = new List<TrainingData>();

    for (int i = 0; i < numSetups; i++)
    {
        var setup = setupGenerator();
        var world = new World(new SpatialHashing(40));

        var trainingSetup = new TrainingSolver.TrainingSetup(world, setup.Source, setup.Target);
        var data = TrainingSolver.SolveSetup(trainingSetup);

        trainingData.Add(data);

        Console.WriteLine($"Completed setup {i + 1}/{numSetups}");

        if (i % batchSize == 0)
        {
            var lines = trainingData.Select(x => x.ToCsvLine());
            var csvText = string.Join(Environment.NewLine, lines) + Environment.NewLine;

            File.AppendAllText(path, csvText);
            trainingData.Clear();

            Console.WriteLine("Saved training data");
        }
    }
    
    if (trainingData.Count > 0)
    {
        var lines = trainingData.Select(x => x.ToCsvLine());
        var csvText = string.Join(Environment.NewLine, lines) + Environment.NewLine;

        File.AppendAllText(path, csvText);
        trainingData.Clear();

        Console.WriteLine("Saved training data");
    }
}

void Train(string trainingDataPath, string modelPath, string testDataPath, int batchSize, int epochs)
{
    var trainingData = File.ReadAllLines(trainingDataPath)
        .Select(TrainingData.FromCsvLine)
        .ToArray();

    var testData = File.ReadAllLines(testDataPath)
        .Select(TrainingData.FromCsvLine)
        .ToArray();

    var model = new BallisticModel("BallisticModel");

    model.Train(trainingData, testData, batchSize, epochs);
    model.save(modelPath);
}

var gameWorld = game.GameWorld;
var gravitySolver = new BarnesHutGravity();

void StartTrainingSetup(TrainingSolver.TrainingSetup setup, float? projectileAngle = null, World? world = null)
{
    var existingBodies = world?.Bodies.ToArray();
    
    if (existingBodies != null)
    {
        gameWorld.AddObjects(existingBodies.Select(x => new Planet(x)));
    }
    
    var gravityObject = new GravityObject(gravitySolver);
    var sourcePlanet = new Planet(setup.Source);
    var targetPlanet = new Planet(setup.Target);
    
    gameWorld.AddObject(gravityObject);
    gameWorld.AddObject(sourcePlanet);
    gameWorld.AddObject(targetPlanet);
    
    if(projectileAngle != null)
    {
        Vector2 projectileDirection = new(MathF.Cos(projectileAngle.Value), MathF.Sin(projectileAngle.Value));
        Vector2 projectilePosition = setup.Source.Position + projectileDirection * 1.1f * setup.Source.Radius;
        Body projectile = new(projectilePosition, new Matter { Hydrogen = 0.1f });

        projectile.Momentum = projectileDirection * projectile.Mass * 40;
        
        gameWorld.AddObject(new Planet(projectile));
    }

    game.Run();
}


/**GenerateTrainingData(() =>
{
    var (source, target) = GameSetup.GenerateSimpleSetup(0, 200, 10, 20, 0, 5);
    
    return new TrainingSolver.TrainingSetup(new World(new SpatialHashing(40)), source, target);
}, 2000, 100, @"C:\Users\Owner\Desktop\training_data.csv");

GenerateTrainingData(() =>
{
    var (source, target) = GameSetup.GenerateSimpleSetup(0, 200, 10, 20, 0, 5);
    
    return new TrainingSolver.TrainingSetup(new World(new SpatialHashing(40)), source, target);
}, 100, 100, @"C:\Users\Owner\Desktop\test_data.csv");**/

//Train(@"C:\Users\Owner\Desktop\training_data.csv", @"C:\Users\Owner\Desktop\model.pt", @"C:\Users\Owner\Desktop\test_data.csv", 100, 400);

var solver = new BarnesHutGravity();
var gravityObject = new GravityObject(solver);
var (star, planets) = GameSetup.GenerateHalo(solver, numPlanets, maxDistance, minDistance, maxMass, minMass, starMass);
var starObject = new Star(star);
var planetObjects = planets.Select(x => new Planet(x)).ToArray();

game.GameWorld.AddObject(gravityObject);
game.GameWorld.AddObject(starObject);
game.GameWorld.AddObjects(planetObjects);

game.Run();