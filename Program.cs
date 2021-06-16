using System;
using Newtonsoft.Json;
using CorpusDraftCSharp;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace DictonaryFormationDummy
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Insert path to the database folder");
            string path = Console.ReadLine();
            var files = new DirectoryInfo(Path.Combine(path, "documents")).GetFiles();
            List<Document> documents = new();
            if (files.Length > 0)
            {
                for (int f = 0; f < files.Length; f++)
                {
                    documents.Add(JsonConvert.DeserializeObject<Document>(File.ReadAllText(files[f].FullName)));
                }
                List<DictionaryUnit> dictionary = new();
                documents = documents.Where(d => d.documentMetaData.Any(f => f.ContainsKey("Tagged") && f["Tagged"].Any(t => t.name != "Not_tagged"))).ToList();
                for (int d = 0; d < documents.Count; d++)
                {
                    Console.WriteLine($"{d.ToString()} - {documents[d].documentName}");
                }
                Console.WriteLine("Print numbers of documents you want to make the dictionary from. Separate by spaces");
                string userChoice = Console.ReadLine();
                List<int> userChosenDocuments = userChoice.Split(' ').ToList().Select(n => Convert.ToInt32(n)).ToList();
                documents = documents.Where(d => userChosenDocuments.Contains(documents.IndexOf(d))).ToList();
                List<Realization> realizationsforDictionary = documents.SelectMany(d => d.texts)
                    .SelectMany(t => t.clauses)
                    .SelectMany(c => c.realizations)
                    .ToList();
                Console.WriteLine("Print the number of lemmatization way you prefer: with UD lemmatizer(2 or any other key), custom lemmatizer(1), or token-as-lemma(0)?");
                List<DictionaryUnit> finalDictionary = new();
                if (Console.ReadLine() == "1")
                {
                    List<string> lemmata = realizationsforDictionary
                    .SelectMany(r => r.realizationFields.Where(t => t.ContainsKey("Lemma"))
                    .SelectMany(t => t["Lemma"])
                    .Select(v => v.name))
                    .Distinct()
                    .ToList();
                    lemmata.ForEach(lemma => finalDictionary.Add(new DictionaryUnit(lemma, realizationsforDictionary.Where(r => r.realizationFields.Where(t => t.ContainsKey("Lemma")).SelectMany(t => t["Lemma"]).Any(v => v.name == lemma)).ToList())));
                }
                else if (Console.ReadLine() == "0")
                {
                    List<string> lemmata = realizationsforDictionary
                    .Select(r => r.lexemeTwo)
                    .Distinct()
                    .ToList();
                    lemmata.ForEach(lemma => finalDictionary.Add(new DictionaryUnit(lemma, realizationsforDictionary.Where(r => r.lexemeTwo == lemma).ToList())));

                }
                else
                {
                    List<string> realizationsForLemmatization = realizationsforDictionary
                        .OrderBy(r => Convert.ToInt32(r.documentID))
                        .ThenBy(r => Convert.ToInt32(r.textID))
                        .ThenBy(r => Convert.ToInt32(r.clauseID))
                        .ThenBy(r => Convert.ToInt32(r.realizationID))
                        .Select(r => r.lexemeTwo)
                        .ToList();
                    string transferredRealizations = String.Join(' ', realizationsForLemmatization);
                    File.WriteAllText(Path.Combine(path, "temp.txt"), transferredRealizations);
                    Console.WriteLine("Use TurkuNLP on the file temp.txt in database directory, then press any key");
                    Console.ReadKey();
                    // collect from .conllu
                    
                }
                finalDictionary = finalDictionary.OrderBy(unit => unit.lemma).ToList();
                // test run
                for (int u = 0; u < finalDictionary.Count; u++)
                {
                    if (!Directory.Exists(Path.Combine(path, "dictionary")))
                    {
                        Directory.CreateDirectory(Path.Combine(path, "dictionary"));
                    }    
                    File.WriteAllText(Path.Combine(path, "dictionary", u.ToString() + ".json"), JsonConvert.SerializeObject(finalDictionary[u]));
                }
            }
            Console.WriteLine("Finished! Press any key to exit...");
            Console.ReadKey();
        }
    }
}
