using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OsuParsers.Database.Objects;
using OsuParsers.Decoders;
using OsuParsers.Enums;
using OsuParsers.Enums.Database;

namespace osu_dbRankFix
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string[] usernames = null;
            if (File.Exists(@"usernames.txt"))
                usernames = File.ReadAllLines(@"usernames.txt");

            Console.Write("osu! folder path: ");
            var osuFolder = Console.ReadLine();

            var scoresDb = DatabaseDecoder.DecodeScores(Path.Combine(osuFolder, "scores.db"));
            var osuDb = DatabaseDecoder.DecodeOsu(Path.Combine(osuFolder, "osu!.db"));
            var osuDbBeatmaps = new Dictionary<string, DbBeatmap>();
            foreach (DbBeatmap beatmap in osuDb.Beatmaps)
            {
                try
                {
                    osuDbBeatmaps.Add(beatmap.MD5Hash, beatmap);
                }
                catch (ArgumentNullException)
                {
                    // beatmap missing MD5Hash
                }
                catch (ArgumentException)
                {
                    // duplicate beatmaps
                }
            }

            foreach (var beatmapTuple in scoresDb.Scores)
            {
                var osuDbBeatmap = osuDbBeatmaps[beatmapTuple.Item1];
                var scores = beatmapTuple.Item2;
                if (usernames != null)
                    scores = scores.Where(score => usernames.Contains(score.PlayerName)).ToList();

                osuDbBeatmap.StandardGrade = scores.Where(score => score.Ruleset == Ruleset.Standard).FirstOrDefault()?.CalculateGrade() ?? Grade.N;
                osuDbBeatmap.TaikoGrade = scores.Where(score => score.Ruleset == Ruleset.Taiko).FirstOrDefault()?.CalculateGrade() ?? Grade.N;
                osuDbBeatmap.CatchGrade = scores.Where(score => score.Ruleset == Ruleset.Fruits).FirstOrDefault()?.CalculateGrade() ?? Grade.N;
                osuDbBeatmap.ManiaGrade = scores.Where(score => score.Ruleset == Ruleset.Mania).FirstOrDefault()?.CalculateGrade() ?? Grade.N;
            }

            osuDb.Save(Path.Combine(osuFolder, "osu!.db"));
            Console.WriteLine("Completed.");
            Console.WriteLine("Press Enter to close");
            Console.ReadLine();
        }

        private static Grade CalculateGrade(this Score score)
        {
            switch (score.Ruleset)
            {
                case Ruleset.Standard:
                    {
                        int totalHits = score.Count50 + score.Count100 + score.Count300 + score.CountMiss;

                        float ratio300 = (float)score.Count300 / totalHits;
                        float ratio50 = (float)score.Count50 / totalHits;

                        if (ratio300 == 1)
                        {
                            if ((score.Mods & Mods.Hidden) == Mods.Hidden || (score.Mods & Mods.Flashlight) == Mods.Flashlight)
                                return Grade.XH;
                            else
                                return Grade.X;
                        }
                        else if (ratio300 > 0.9 && ratio50 <= 0.01 && score.CountMiss == 0)
                        {
                            if ((score.Mods & Mods.Hidden) == Mods.Hidden || (score.Mods & Mods.Flashlight) == Mods.Flashlight)
                                return Grade.SH;
                            else
                                return Grade.S;
                        }
                        else if ((ratio300 > 0.8 && score.CountMiss == 0) || ratio300 > 0.9)
                            return Grade.A;
                        else if ((ratio300 > 0.7 && score.CountMiss == 0) || ratio300 > 0.8)
                            return Grade.B;
                        else if (ratio300 > 0.6)
                            return Grade.C;
                        else
                            return Grade.D;
                    }
                case Ruleset.Taiko:
                    {
                        int totalHits = score.Count50 + score.Count100 + score.Count300 + score.CountMiss;

                        float ratio300 = (float)score.Count300 / totalHits;
                        float ratio50 = (float)score.Count50 / totalHits;

                        if (ratio300 == 1)
                        {
                            if ((score.Mods & Mods.Hidden) == Mods.Hidden || (score.Mods & Mods.Flashlight) == Mods.Flashlight)
                                return Grade.XH;
                            else
                                return Grade.X;
                        }
                        else if (ratio300 > 0.9 && ratio50 <= 0.01 && score.CountMiss == 0)
                        {
                            if ((score.Mods & Mods.Hidden) == Mods.Hidden || (score.Mods & Mods.Flashlight) == Mods.Flashlight)
                                return Grade.SH;
                            else
                                return Grade.S;
                        }
                        else if ((ratio300 > 0.8 && score.CountMiss == 0) || ratio300 > 0.9)
                            return Grade.A;
                        else if ((ratio300 > 0.7 && score.CountMiss == 0) || ratio300 > 0.8)
                            return Grade.B;
                        else if (ratio300 > 0.6)
                            return Grade.C;
                        else
                            return Grade.D;
                    }
                case Ruleset.Fruits:
                    {
                        int totalHits = score.Count50 + score.Count100 + score.Count300 + score.CountMiss + score.CountKatu;
                        double accuracy = totalHits > 0 ? (double)(score.Count50 + score.Count100 + score.Count300) / totalHits : 1;

                        if (accuracy == 1)
                        {
                            if ((score.Mods & Mods.Hidden) == Mods.Hidden || (score.Mods & Mods.Flashlight) == Mods.Flashlight)
                                return Grade.XH;
                            else
                                return Grade.X;
                        }
                        else if (accuracy > 0.98)
                        {
                            if ((score.Mods & Mods.Hidden) == Mods.Hidden || (score.Mods & Mods.Flashlight) == Mods.Flashlight)
                                return Grade.SH;
                            else
                                return Grade.S;
                        }
                        else if (accuracy > 0.94)
                            return Grade.A;
                        else if (accuracy > 0.9)
                            return Grade.B;
                        else if (accuracy > 0.85)
                            return Grade.C;
                        else
                            return Grade.D;
                    }
                case Ruleset.Mania:
                    {
                        int totalHits = score.Count50 + score.Count100 + score.Count300 + score.CountMiss + score.CountGeki + score.CountKatu;
                        double accuracy = totalHits > 0 ? (double)(score.Count50 * 50 + score.Count100 * 100 + score.CountKatu * 200 + (score.Count300 + score.CountGeki) * 300) / (totalHits * 300) : 1;

                        if (accuracy == 1)
                        {
                            if ((score.Mods & Mods.Hidden) == Mods.Hidden || (score.Mods & Mods.Flashlight) == Mods.Flashlight)
                                return Grade.XH;
                            else
                                return Grade.X;
                        }
                        else if (accuracy > 0.95)
                        {
                            if ((score.Mods & Mods.Hidden) == Mods.Hidden || (score.Mods & Mods.Flashlight) == Mods.Flashlight)
                                return Grade.SH;
                            else
                                return Grade.S;
                        }
                        else if (accuracy > 0.9)
                            return Grade.A;
                        else if (accuracy > 0.8)
                            return Grade.B;
                        else if (accuracy > 0.7)
                            return Grade.C;
                        else
                            return Grade.D;
                    }
            }
            return Grade.N;
        }
    }
}
