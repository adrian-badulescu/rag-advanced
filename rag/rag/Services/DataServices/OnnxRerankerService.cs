using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Starlings.Tokenizers;
using rag.Models;
using rag.Models.Db;

namespace rag.Services;

public class OnnxRerankerService : IOnnxRerankerService, IDisposable
{
    public readonly InferenceSession _session;
    public readonly Tokenizer _tokenizer;
    public readonly LLMConfig _options;
    
    public OnnxRerankerService(IOptions<LLMConfig> options)
    {
        _options = options.Value;
        
        string modelDirectory = Path.Combine(AppContext.BaseDirectory, "Models", "RerankerModel");
        
        Console.WriteLine($"here { modelDirectory }");
        
        string modelPath = Path.Combine(modelDirectory, "model.onnx");
        string tokenizerPath = Path.Combine(modelDirectory, "tokenizer.json"); 
        
        if (!File.Exists(modelPath) || !File.Exists(tokenizerPath))
        {
            throw new FileNotFoundException($"No files found in directory: {modelDirectory}");
        }

        _session = new InferenceSession(modelPath);

        using Stream tokenizerStream = File.OpenRead(tokenizerPath);
        _tokenizer = Tokenizer.FromFile(tokenizerPath)
            .WithPadding(new PaddingOptions(PadId:1, PadToken:"<|padding|>"));
    }
    
    public List<TextChunkWithScore> Rank(string query, List<TextChunk> candidates, int topK = 3)
    {
        if (candidates == null || !candidates.Any()) return new();

        var scoredResults = new List<TextChunkWithScore>();
        
        foreach (var candidate in candidates)
        {
            string combinedText = $"{query} [SEP] {candidate.Content}";
            
            var encodedResult = _tokenizer.Encode(combinedText);
            
            long[] ids = [.. encodedResult.Ids.Select(id => (long)id)];
            long[] attentionMask = [.. encodedResult.AttentionMask.Select(m => (long)m)];

            if (ids.Length == 0) continue;
            
            int batchSize = 1;
            int sequenceLength = ids.Length;

            var inputIdsTensor = new DenseTensor<long>(ids, new[] { batchSize, sequenceLength });
            var attentionMaskTensor = new DenseTensor<long>(attentionMask, new[] { batchSize, sequenceLength });

            var inputs = new NamedOnnxValue[]
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor)
            };

            using var results = _session.Run(inputs);
            var output = results.First().AsTensor<float>();

            float logit = output[0];
            float score = Sigmoid(logit);

            scoredResults.Add(new TextChunkWithScore
            {
                Chunk = candidate,
                Score = score
            });
            Console.WriteLine($"Candidate: {candidate.Content.Substring(0, Math.Min(30, candidate.Content.Length))}... | Score: {score}");
        }
        
        return scoredResults
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();
    }

    public void Dispose()
    {
        _session.Dispose();
    }
    
    private static float Sigmoid(float value)
    {
        return 1.0f / (1.0f + (float)Math.Exp(-value));
    }
}