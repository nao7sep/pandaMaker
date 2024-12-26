using System.Text;

using yyLib;
using yyGptLib;

namespace pandaMaker;

class Program
{
    static void Main (string [] args)
    {
        var xConnectionInfo = new yyGptImagesConnectionInfo { ApiKey = yyUserSecrets.Default.OpenAi!.ApiKey! };

        string [] xMessages = ["OK!", "Sure!", "OMG!", "No problem!", "Thank you!", "Sorry!", "No way!"];

        foreach (string xMessage in xMessages)
        {
            Parallel.For (0, 10,
                new ParallelOptions { MaxDegreeOfParallelism = 1 }, // Rate limit appears to be easy to reach
                x =>
            {
                var xRequest = new yyGptImagesRequest
                {
                    Prompt = $"A cute or funny or arrogant panda that says '{xMessage}'. Please dont make the image photorealistic. The image must contain only one panda in a various place. Please make the image happy and friendly.",

                    Model = "dall-e-3",
                    Quality = "hd",
                    ResponseFormat = "b64_json",
                    Size = "1024x1024"
                };

                using (yyGptImagesClient xClient = new (xConnectionInfo))
                {
                    try
                    {
                        var xSendingTask = xClient.SendAsync (xRequest);
                        xSendingTask.Wait ();

                        var xReadingTask = xClient.ReadToEndAsync ();
                        xReadingTask.Wait ();

                        string? xJson = xReadingTask.Result;
                        var xResponse1 = yyGptImagesResponseParser.Parse (xJson);

                        if (xSendingTask.Result.HttpResponseMessage.IsSuccessStatusCode)
                        {
                            byte [] xBytes = Convert.FromBase64String (xResponse1.Data! [0].B64Json!);

                            string xFilePathWithoutExtension = yyAppDirectory.MapPath ($"Images{Path.DirectorySeparatorChar}{yyFormatter.ToRoundtripFileNameString (DateTime.UtcNow)}");

                            yyDirectory.CreateParent (xFilePathWithoutExtension);
                            File.WriteAllText (xFilePathWithoutExtension + ".txt", xResponse1.Data [0].RevisedPrompt, Encoding.UTF8);
                            File.WriteAllBytes (xFilePathWithoutExtension + ".png", xBytes);

                            Console.WriteLine ("Generated image: " + xFilePathWithoutExtension + ".png");
                        }

                        else Console.WriteLine (xJson.GetVisibleString ());
                    }

                    catch (Exception xException)
                    {
                        yyLogger.Default.TryWriteException (xException);
                        Console.WriteLine (xException.ToString ());
                    }
                }
            });
        }
    }
}
