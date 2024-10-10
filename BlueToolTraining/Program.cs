using System;
using System.Collections.Generic;
using System.Linq;
using ViDi2.Training.Local;
//Must be run on an x64 platform.
//Add NuGet packages from: C:\ProgramData\Cognex\VisionPro Deep Learning\3.3\Examples\packages
    //ViDi.NET
    //ViDi.NET.VisionPro
namespace BlueToolTraining
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Initialize workspace directory
            ViDi2.Training.Local.WorkspaceDirectory workspaceDir = new ViDi2.Training.Local.WorkspaceDirectory();
            //Set the path to workspace directory
            workspaceDir.Path = @"C:\Users\acooper\Desktop\Training";

            //Create a library access instance using the workspace directory
            using (LibraryAccess libraryAccess = new LibraryAccess(workspaceDir))
            {
                //Create a control interface for training tools
                using (ViDi2.Training.IControl myControl = new ViDi2.Training.Local.Control(libraryAccess))
                {
                    //Create a new workspace and add it to the control
                    ViDi2.Training.IWorkspace myWorkspace = myControl.Workspaces.Add("myBlueWorkspace");

                    //Add a new stream to the workspace
                    ViDi2.Training.IStream myStream = myWorkspace.Streams.Add("default");

                    //Add a Blue Tool to the stream (for defect detection)
                    ViDi2.Training.IBlueTool myBlueTool = myStream.Tools.Add("Locate", ViDi2.ToolType.Blue) as ViDi2.Training.IBlueTool;

                    //Define valid image file extensions
                    List<string> ext = new List<string> { ".jpg", ".bmp", ".png" };

                    //Get all image files in the specified directory that match the extensions
                    IEnumerable<string> imageFiles = System.IO.Directory.GetFiles(
                        @"C:\Users\acooper\Desktop\Training\BlueToolTraining\BlueToolTraining\Images",
                        "*.*",
                        System.IO.SearchOption.TopDirectoryOnly
                    ).Where(s => ext.Any(e => s.EndsWith(e)));

                    //Add each image to the stream's database
                    foreach (string file in imageFiles)
                    {
                        using (ViDi2.FormsImage image = new ViDi2.FormsImage(file))
                        {
                            myStream.Database.AddImage(image, System.IO.Path.GetFileName(file));
                        }
                    }

                    //Process all images in the Blue Tool's database
                    myBlueTool.Database.Process();
                    myBlueTool.Wait(); // Wait until the processing is done

                    //Map of feature positions for specific image files
                    Dictionary<string, ViDi2.Point> featurePositions = new Dictionary<string, ViDi2.Point>
                    {
                        { "Bad (1).png", new ViDi2.Point(1101, 1029) },
                        { "Bad (11).png", new ViDi2.Point(1157, 1021) },
                        { "Bad (14).png", new ViDi2.Point(1125, 1035) },
                        { "Good (1).png", new ViDi2.Point(1087, 1030) },
                        { "Good (10).png", new ViDi2.Point(1103, 1138) },
                        { "Good (22).png", new ViDi2.Point(1115, 1031) },
                        { "Good (35).png", new ViDi2.Point(1180, 1052) },
                        { "Good (38).png", new ViDi2.Point(1045, 1023) },
                        { "Good (50).png", new ViDi2.Point(1171, 1052) },
                        { "Good (51).png", new ViDi2.Point(1233, 1089) }
                    };

                    //Label samples with corresponding feature positions
                    foreach (ViDi2.Training.SortedViewKey sample in myBlueTool.Database.List().ToList())
                    {
                        if (featurePositions.ContainsKey(sample.SampleName))
                        {
                            myBlueTool.Database.AddFeature(
                                sample,
                                "chip",
                                featurePositions[sample.SampleName],
                                0.0,
                                1.0
                            );
                        }
                    }

                    //Set various parameters for the Blue Tool
                    myBlueTool.Parameters.FeatureSize = new ViDi2.Size(932, 676);
                    myBlueTool.Parameters.Rotation = new List<ViDi2.Interval> { new ViDi2.Interval(0, 2 * Math.PI) };
                    myBlueTool.Parameters.Scale = 0.05;
                    myBlueTool.Parameters.AspectRatio = 0.05;
                    myBlueTool.Parameters.Shear = 0.05;
                    myBlueTool.Parameters.Flip = ViDi2.FlippingMode.Both;
                    myBlueTool.Parameters.Luminance = 0.05;
                    myBlueTool.Parameters.Contrast = 0.05;
                    myBlueTool.Parameters.CountEpochs = 10;  // Set training epochs

                    //Mark the dataset as ready for training
                    myBlueTool.Database.SetTrainFlag("", true);

                    //Start training the Blue Tool
                    myBlueTool.Train();
                    Console.WriteLine("Starting:");

                    //Monitor the progress of the training
                    while (!myBlueTool.Wait(1000))
                    {
                        Console.WriteLine(myBlueTool.Progress.Description + " " + myBlueTool.Progress.ETA.ToString());
                    }

                    //Process the database again after training
                    myBlueTool.Database.Process();
                    myBlueTool.Wait();

                    //Export the runtime workspace to a file
                    using (System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\acooper\Desktop\Training\BlueToolRuntime.vrws", System.IO.FileMode.Create))
                    {
                        myWorkspace.ExportRuntimeWorkspace().CopyTo(fs);
                    }

                    //Save the workspace
                    myWorkspace.Save();
                }
            }
        }
    }
}
