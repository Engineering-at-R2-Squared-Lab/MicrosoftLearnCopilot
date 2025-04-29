using System;
using Microsoft.SemanticKernel.ChatCompletion;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MicrosoftLearnCopilot.Backend.Model;

public class ChatHistoryModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    [BsonElement("history")]
    public ChatHistory history { get; set; }
}
