using System.ComponentModel;
using System.Numerics;
using OsuParsers.Beatmaps;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Beatmaps.Objects.Taiko;
using OsuParsers.Decoders;
using OsuParsers.Enums;
using OsuParsers.Enums.Beatmaps;

static List<string> GetCountedHitsAsList(Beatmap beatmap, bool printUncountedHits, bool printBeatInt)
{
    List<string> hits = new List<string>();

    int length = beatmap.HitObjects.Count;
    string hitType = "";
    int hitInt = 0;

    for (int i = 0; i < length; i++)
    {
        String iHitSound = beatmap.HitObjects[i].HitSound.ToString();
        String iTypeName = beatmap.HitObjects[i].GetType().Name;
        if (iHitSound == "Whistle" || iHitSound == "Clap" && iTypeName == "TaikoHit")
        {
            hitType = "Blue";
            hitInt = 1;
        }
        else if (iHitSound == "None" && iTypeName == "TaikoSpinner")
        {
            hitType = "Spinner";
            hitInt = 4;
        }
        else if (iHitSound == "None" && iTypeName == "TaikoDrumroll")
        {
            hitType = "Drumroll";
            hitInt = 5;
        }
        else if (iHitSound == "None" && iTypeName == "TaikoHit")
        {
            hitType = "Red";
            hitInt = 0;
        }
        else if (iHitSound == "Finish" && iTypeName == "TaikoHit")
        {
            hitType = "BigRed";
            hitInt = 2;
        }
        else if (iHitSound == "6" || iHitSound == "12" && iTypeName == "TaikoHit")
        {
            hitType = "BigBlue";
            hitInt = 3;
        }

        if (printUncountedHits)
        {
            Console.WriteLine(
                beatmap.HitObjects[i].StartTime + " " +
                Convert.ToString((int)beatmap.HitObjects[i].HitSound, 2) + " " +
                beatmap.HitObjects[i].GetType().Name);
        }

        if (printBeatInt)
        {
            Console.WriteLine(beatmap.HitObjects[i].StartTime + " " + hitInt);
        }

        if (iTypeName != "TaikoDrumroll")
        {
            hits.Add(beatmap.HitObjects[i].StartTime + " " + hitInt + " " + "0");
        }
        else
        {
            HitObject obj = beatmap.HitObjects[i];
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
            {
                string name = descriptor.Name;
                object value = descriptor.GetValue(obj);
                if (name == "PixelLength")
                {
                    hits.Add(beatmap.HitObjects[i].StartTime + " " + hitInt + " " + value);
                }
            }
        }
    }
    return hits;
}

static void WriteListToTxt(List<string> list, String fileName)
{
    string lastItem = list.Last();
    string[] lastItemAsArray = lastItem.Split(' ');
    int maxTrackSplitNum = Convert.ToInt32(Math.Ceiling(Double.Parse(lastItemAsArray[0]) / 10000.0));

    Dictionary<int, List<string>> splitList = new Dictionary<int, List<string>>();
    for (int i = 0; i < maxTrackSplitNum; i++)
    {
        splitList.Add(i, new List<string>());
        // Console.WriteLine(i.ToString());
    }

    foreach (string line in list)
    {
        string[] lineAsArray = line.Split(' ');
        int trackSplitNum = Convert.ToInt32(Math.Ceiling(Double.Parse(lineAsArray[0]) / 10000.0));
        if (trackSplitNum != 0)
        {
            splitList[trackSplitNum - 1].Add(line);
        }
    }

    foreach (KeyValuePair<int, List<string>> item in splitList)
    {
        // Console.WriteLine("Key: {0}, Value: {1}", item.Key, String.Join(", ", item.Value));
        File.WriteAllLines(fileName + "_" + item.Key + ".txt", item.Value);
    }
}

static Beatmap WriteTxtToNewBeatmap(List<String> txtList)
{
    // Create new beatmap
    Beatmap newBeatmap = new Beatmap();
    newBeatmap.GeneralSection.Mode = Ruleset.Taiko;
    newBeatmap.GeneralSection.ModeId = 1;
    int length = txtList.Count;

    for (int i = 0; i < length; i++)
    {
        // Split string to new list
        List<String> oneHit = txtList[i].Split(' ').ToList();
        //Console.WriteLine(txtList[i]);
        //Console.WriteLine(oneHit[0] + ", " + oneHit[1]);

        HitSoundType oneHitSound = HitSoundType.None;
        Extras extras = new Extras();
        String hitObjectType = "";
        if (oneHit[1] == "0")
        {
            hitObjectType = "TaikoHit";
        }
        if (oneHit[1] == "1")
        {
            oneHitSound = HitSoundType.Whistle;
            hitObjectType = "TaikoHit";
        }
        if (oneHit[1] == "2")
        {
            oneHitSound = HitSoundType.Finish;
            hitObjectType = "TaikoHit";
        }
        if (oneHit[1] == "3")
        {
            oneHitSound = HitSoundType.Whistle;
            oneHitSound += (int)HitSoundType.Finish;
            hitObjectType = "TaikoHit";
        }
        if (oneHit[1] == "4")
        {
            // hitObjectType = "TaikoHit";
            // hitObjectType = "TaikoSpinner";
        }
        if (oneHit[1] == "5")
        {
            // hitObjectType = "TaikoHit";
            // hitObjectType = "TaikoSpinner";
            // hitObjectType = "TaikoDrumroll";
        }

        if (hitObjectType == "TaikoHit")
        {
            newBeatmap.HitObjects.Add(new TaikoHit(Vector2.Zero, Int32.Parse(oneHit[0]), 0, oneHitSound, extras, false, 0));
        }
        if (hitObjectType == "TaikoSpinner")
        {
            newBeatmap.HitObjects.Add(new TaikoSpinner(Vector2.Zero, Int32.Parse(oneHit[0]), 0, oneHitSound, extras, false, 0));
        }
        if (hitObjectType == "TaikoDrumroll")
        {
            //Console.WriteLine("Drumroll");
            //Console.WriteLine(oneHit[2]);
            CurveType curveType = CurveType.PerfectCurve;
            List<Vector2> sliderPoints = new List<Vector2>();
            double pixelLength = Convert.ToDouble(oneHit[2]);
            List<HitSoundType> edgeHitSounds = null;
            List<Tuple<SampleSet, SampleSet>> edgeAdditions = null;
            newBeatmap.HitObjects.Add(new TaikoDrumroll(Vector2.Zero, Int32.Parse(oneHit[0]), Int32.Parse(oneHit[0]), oneHitSound, curveType, sliderPoints, 0, pixelLength, edgeHitSounds, edgeAdditions, extras, false, 0));
        }
    }
    return newBeatmap;
}

static void BeatmapToTxt(string path)
{
    string[] folders = Directory.GetDirectories(path);
    foreach (string folder in folders)
    {
        string[] files = Directory.GetFiles(folder);
        foreach (var item in files)
        {
            if (item.EndsWith(".osu"))
            {
                Console.WriteLine(item);
                Beatmap beatmap = BeatmapDecoder.Decode(item);

                string txtItem = item.Replace(".osu", "");
                WriteListToTxt(GetCountedHitsAsList(beatmap, false, false), txtItem);
            }
        }
    }
}

static void renameMp3(string path)
{
    string[] folders = Directory.GetDirectories(path);
    foreach (string folder in folders)
    {
        string[] files = Directory.GetFiles(folder);
        foreach (var item in files)
        {
            if (item.EndsWith(".mp3") && item.Contains(".osu"))
            {
                string source = item;
                // Remove a substring from the middle of the string.
                string toRemove = ".osu";
                string result = string.Empty;
                int i = source.IndexOf(toRemove);
                if (i >= 0)
                {
                    result = source.Remove(i, toRemove.Length);
                }
                File.Move(item, result);
            }
        }
    }
}

static void deteleAllTxt(string path)
{
    string[] folders = Directory.GetDirectories(path);
    foreach (string folder in folders)
    {
        string[] files = Directory.GetFiles(folder);
        foreach (var item in files)
        {
            if (item.EndsWith(".txt"))
            {
                File.Delete(item);
            }
        }
    }
}

static void export_new_beatmap(string txtpath)
{
    List<string> allLinesText = File.ReadAllLines(txtpath).ToList();
    Beatmap newBeatmap = WriteTxtToNewBeatmap(allLinesText);
    newBeatmap.MetadataSection.Title = "GodTest";
    newBeatmap.GeneralSection.AudioFilename = "Polyphia - Playing God (Official Music Video).mp3";
    TimingPoint FakeTimingPoint = new TimingPoint();
    FakeTimingPoint.BeatLength = 400.0f;
    FakeTimingPoint.CustomSampleSet = 0;
    FakeTimingPoint.Inherited = false;
    FakeTimingPoint.Effects = Effects.None;
    FakeTimingPoint.Offset = 0;
    FakeTimingPoint.SampleSet = SampleSet.Normal;
    FakeTimingPoint.TimeSignature = TimeSignature.SimpleQuadruple;
    FakeTimingPoint.Volume = 100;
    newBeatmap.TimingPoints.Add(FakeTimingPoint);
    newBeatmap.Save(@"pathToNewBeatmap.osu");
}

string datapath = "SR";
string txtpath = "test.txt";

// renameMp3(datapath);
// deteleAllTxt(datapath);

// BeatmapToTxt(datapath);

export_new_beatmap(txtpath);
