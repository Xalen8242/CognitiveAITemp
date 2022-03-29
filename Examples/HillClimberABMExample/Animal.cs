/**

This code represents the decision making and information for a single hill climber agent.

What does it do: Upon each step the agent moves to what it thinks is the next best location, based upon either the perceptron output or the q-learning module.

This code was originally generated by the Mars library, then we altered it to use the perceptron as a part of it's decision making process.

*/

namespace HillClimberExample
{
    using System;
    using Mars.Interfaces.Layer;
    using Mars.Components.Environments;
    using Mars.Common.Logging;
    using System.Collections.Generic;
    using CognitiveABM.Perceptron;
    using CognitiveABM.QLearning;
    using System.IO;


    public class Animal : Mars.Interfaces.Agent.IMarsDslAgent
    {
        private static readonly ILogger _Logger = LoggerFactory.GetLogger(typeof(Animal));


        private readonly float[] AgentMemory;

        private readonly int startingElevation;

        public Guid ID { get; }

        public Mars.Interfaces.Environment.Position Position { get; set; }

        public bool Equals(Animal other) => Equals(ID, other.ID);

        public override int GetHashCode() => ID.GetHashCode();

        public QLearning qLearn = new QLearning();

        public int tickNum = 0;

        private string rule = default;

        public string Rule
        {
            get => rule;
            set
            {
                if (rule != value) rule = value;
            }
        }

        private int animalId = default;

        public int AnimalId
        {
            get => animalId;
            set
            {
                if (animalId != value) animalId = value;
            }
        }

        private int bioEnergy = default;

        public int BioEnergy
        {
            get => bioEnergy;
            set
            {
                if (bioEnergy != value) bioEnergy = value;
            }
        }

        private int elevation = default;

        public int Elevation
        {
            get => elevation;
            set
            {
                if (elevation != value) elevation = value;
            }
        }

        internal int executionFrequency;

        public Terrain Terrain { get; set; }

        [Mars.Interfaces.LIFECapabilities.PublishForMappingInMars]
        public Animal(Guid _id, Terrain _layer, RegisterAgent _register, UnregisterAgent _unregister, SpatialHashEnvironment<Animal> _AnimalEnvironment, int AnimalId, double xcor = 0, double ycor = 0, int freq = 1)
        {
            ID = _id;
            Terrain = _layer;
            this.AnimalId = AnimalId;
            executionFrequency = freq;

            Position = Mars.Interfaces.Environment.Position.CreatePosition(xcor, ycor);
            var pos = InitialPosition();
            Position = Mars.Interfaces.Environment.Position.CreatePosition(pos.Item1, pos.Item2);

            Terrain._AnimalEnvironment.Insert(this);
            _register(_layer, this, freq);


            Elevation = Terrain.GetIntegerValue(Position.X, Position.Y);
            startingElevation = Elevation;

            //AgentMemory is functionally useless right now
            //it goes into perceptron, but it's useage is commented out
            AgentMemory = new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        }

        // Tick function is called on each step of the simulation
        public void Tick()
        {

            /**FCM*/
            //-----FCM----//
            // PerceptronFactory perceptron = new PerceptronFactory(9, 9, 1, 9);
            // float[] outputs = perceptron.CalculatePerceptronFromId(AnimalId, inputs, AgentMemory);
            // outputs.CopyTo(AgentMemory, 0);
            // outputs.CopyTo(AgentMemory, outputs.Length);
            // //more want outputs, aka a list of floats
            //List<int[]> locations = GetAdjacentTerrainPositions();//leave alone
            //
            // int highestOutput = 0;
            // for (int i = 0; i < 9; i++)
            // {
            //     if (outputs[i] > outputs[highestOutput])
            //     {
            //         highestOutput = i;
            //     }
            // }
            //int[] newLocation = locations[highestOutput];

            /**QLearn*/
            List<int[]> adjacentTerrainLocations = GetAdjacentTerrainPositions();
            float[] adjacentTerrainElevations = GetAdjacentTerrainElevations();
            //change terrainElevations into a matrix
            //adjacentTerrainElevations contains 9 elements, so we need 3x3 matrix
            int index = 0;
            float[,] landscapePatch = new float[3, 3];
            float min = adjacentTerrainElevations[index];
            float max = adjacentTerrainElevations[index];
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if(adjacentTerrainElevations[index] < min){
                      min = adjacentTerrainElevations[index];
                    }
                    if(adjacentTerrainElevations[index] > max){
                      max = adjacentTerrainElevations[index];
                    }
                    landscapePatch[x, y] = adjacentTerrainElevations[index];
                    index++;
                }
            }
            int direction = this.qLearn.getDirection(landscapePatch, min, max);
            int[] newLocation = adjacentTerrainLocations[direction];


            //MoveTo (animal object, location, traveling distance)
            Terrain._AnimalEnvironment.MoveTo(this, newLocation[0], newLocation[1], 1, predicate: null);
            int xPos = (int)Position.X;
            int yPos = (int)Position.Y;
            
            int tempElevation = Elevation;
            Elevation = Terrain.GetIntegerValue(this.Position.X, this.Position.Y);
            this.qLearn.getNewFit(Elevation, tempElevation, this.AnimalId, this.tickNum, landscapePatch, export: true, xPos, yPos);
            BioEnergy = (Elevation < 0) ? 0 : Elevation;
            this.tickNum++;
        }

        // helper methods

        private Tuple<int, int> InitialPosition()
        {
            var random = new Random(18);
            //var random = new Random(ID.GetHashCode()); //using hard coded value for testing
            return new Tuple<int, int>(random.Next(Terrain.DimensionX()), random.Next(Terrain.DimensionY()));
        }

        private float[] GetAdjacentTerrainElevations()
        {
            List<float> elevations = new List<float>();
            int x = (int)Position.X;
            int y = (int)Position.Y;

            for (int dx = -1; dx <= 1; ++dx)
            {
                for (int dy = -1; dy <= 1; ++dy)
                {
                    elevations.Add((float)Terrain.GetRealValue(dx + x, dy + y));
                }
            }

            return elevations.ToArray();
        }

        private List<int[]> GetAdjacentTerrainPositions()
        {
            List<int[]> locations = new List<int[]>();
            int x = (int)Position.X;
            int y = (int)Position.Y;

            for (int dx = -1; dx <= 1; ++dx)
            {
                for (int dy = -1; dy <= 1; ++dy)
                {
                    int[] location = new int[] { dx + x, dy + y };
                    locations.Add(location);
                }
            }

            return locations;
        }


    }
}
