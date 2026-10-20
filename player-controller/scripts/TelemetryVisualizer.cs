using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

[Tool]
public partial class TelemetryVisualizer : Node3D
{
	// Match the data structure you saved
    public class JumpTelemetry
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public bool IsHanging { get; set; }
    }

    public override void _Notification(int what){
		if (what == NotificationSceneInstantiated){
			VisualizeAllSession();
		}
	}

    private void VisualizeAllSession()
    {

		foreach (Node child in GetChildren())
        {
            if (child.Name.ToString().StartsWith("TelemetryMarker"))
            {
                child.QueueFree();
            }
        }

        string folderPath = ProjectSettings.GlobalizePath("res://data/");
        if (!Directory.Exists(folderPath))
        {
            GD.PrintErr("[Visualizer] Data folder not found!");
            return;
        }

        // Find all session files in your data folder
        string[] files = Directory.GetFiles(folderPath, "jump_session_*.json");
        if (files.Length == 0)
        {
            GD.Print("[Visualizer] No telemetry session files found.");
            return;
        }

        // Sort files to find the most recent one
        //Array.Sort(files, (a, b) => File.GetCreationTime(b).CompareTo(File.GetCreationTime(a)));
        //string latestFile = files[0];

       // GD.Print($"[Visualizer] Loading telemetry from: {latestFile}");

		foreach (string file in files){

			try
			{
				string jsonString = File.ReadAllText(file);
				var logs = JsonSerializer.Deserialize<List<JumpTelemetry>>(jsonString);

				if (logs != null)
				{
					foreach (var jump in logs)
					{
						SpawnJumpMarker(jump);
					}
					GD.Print($"[Visualizer] Successfully spawned {logs.Count} jump markers.");
				}
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[Visualizer] Failed to read/parse telemetry file: {ex.Message}");
			}
		}
    }

    private void SpawnJumpMarker(JumpTelemetry jump)
    {
        //Create the Mesh Instance and Sphere Mesh
        var meshInstance = new MeshInstance3D();
        var sphere = new SphereMesh
        {
            Radius = 0.3f,
            Height = 0.6f
        };
        meshInstance.Mesh = sphere;

        //  Create a material for color-coding
        var material = new StandardMaterial3D();
        if (jump.IsHanging)
        {
            material.AlbedoColor = Colors.Red; // Matches your "jump red" log condition
        }
        else
        {
            material.AlbedoColor = Colors.Cyan; // Normal or non-hanging jumps
        }

        // Make it unshaded so it stands out brightly against any lighting
        material.ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded;
        meshInstance.MaterialOverride = material;

        //  Add it to the world
        AddChild(meshInstance);

        // Position the sphere at the recorded coordinates
        meshInstance.GlobalPosition = new Vector3(jump.X, jump.Y, jump.Z);

    }
}
