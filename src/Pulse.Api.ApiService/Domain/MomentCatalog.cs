using System.Security.Cryptography;
using System.Text;

namespace Pulse.Api.ApiService.Domain;

/// <summary>
/// The built-in catalogue of packs and their moment templates. This is the single source of truth for the
/// seed data: <see cref="Packs"/> / <see cref="Templates"/> are applied via EF <c>HasData</c> in the
/// model configuration, so ids are deterministic (derived from stable keys) to keep migrations stable.
///
/// The daily Moment is drawn from the free <c>core</c> pack only. Pro packs are listed in the (future)
/// store but locked. Per-category emoji/colour for the UI is resolved client-side from the category.
/// </summary>
public static class MomentCatalog
{
    /// <summary>The slug of the free pack that the daily Moment is drawn from.</summary>
    public const string CorePackKey = "core";

    /// <summary>
    /// Deterministic GUID for a seed row from a namespace + key, so re-running migrations produces the
    /// same ids. (MD5-based v3-style UUID — value stability matters here, not cryptographic strength.)
    /// </summary>
    public static Guid StableId(string @namespace, string key)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"{@namespace}:{key}"));
        return new Guid(bytes);
    }

    public sealed record PackSeed(string Key, string Title, string Emoji, bool IsPro, int SortOrder)
    {
        public Guid Id => StableId("pack", Key);
    }

    public sealed record TemplateSeed(
        string PackKey, string Key, MomentCategory Category, string Title, string Prompt, MomentResponseKind ResponseKind,
        IReadOnlyList<string>? Options = null)
    {
        public Guid Id => StableId("template", $"{PackKey}/{Key}");
        public Guid PackId => StableId("pack", PackKey);
    }

    public static readonly IReadOnlyList<PackSeed> Packs =
    [
        new("core", "Core", "✨", IsPro: false, SortOrder: 0),
        new("photography", "Photography", "📸", IsPro: true, SortOrder: 1),
        new("adventure", "Adventure", "🌍", IsPro: true, SortOrder: 2),
        new("fun", "Fun", "😂", IsPro: true, SortOrder: 3),
        new("reflection", "Reflection", "🧠", IsPro: true, SortOrder: 4),
        new("romance", "Romance", "💌", IsPro: true, SortOrder: 5),
        new("garden", "Garden", "🌱", IsPro: true, SortOrder: 6),
    ];

    public static readonly IReadOnlyList<TemplateSeed> Templates =
    [
        // --- Core pack (free, ~35 well-spaced moments across categories) ---
        new("core", "smile", MomentCategory.Capture, "Smile", "Photograph something that made you smile today.", MomentResponseKind.Photo),
        new("core", "colour", MomentCategory.Capture, "Favourite colour", "Capture something that's your favourite colour.", MomentResponseKind.Photo),
        new("core", "tiny-detail", MomentCategory.Capture, "Tiny detail", "Find beauty in something most people would walk past.", MomentResponseKind.Photo),
        new("core", "seasons", MomentCategory.Capture, "Seasons", "Capture today's weather.", MomentResponseKind.Photo),
        new("core", "one-photo", MomentCategory.Micro, "One photo", "One photo. No caption.", MomentResponseKind.Photo),

        new("core", "perfect-sunday", MomentCategory.Draw, "Perfect Sunday", "Draw your perfect Sunday.", MomentResponseKind.Drawing),
        new("core", "dream-house", MomentCategory.Draw, "Dream house", "Sketch your dream home.", MomentResponseKind.Drawing),
        new("core", "self-portrait", MomentCategory.Draw, "Self portrait", "Draw yourself from memory.", MomentResponseKind.Drawing),
        new("core", "guess-animal", MomentCategory.Draw, "Guess", "Both draw an animal. Reveal together.", MomentResponseKind.Drawing),
        new("core", "one-doodle", MomentCategory.Micro, "One doodle", "Draw one thing.", MomentResponseKind.Drawing),

        new("core", "appreciation", MomentCategory.LoveLetter, "Appreciation", "One thing I appreciated today.", MomentResponseKind.Text),
        new("core", "thank-you", MomentCategory.LoveLetter, "Thank you", "Something you've never thanked them for.", MomentResponseKind.Text),
        new("core", "favourite-week", MomentCategory.LoveLetter, "Favourite", "My favourite thing about you this week.", MomentResponseKind.Text),
        new("core", "looking-forward", MomentCategory.LoveLetter, "Future", "One thing I'm looking forward to.", MomentResponseKind.Text),

        new("core", "this-or-that", MomentCategory.Fun, "This or That", "Pizza or Burger? Pick your favourite.", MomentResponseKind.Choice, ["🍕 Pizza", "🍔 Burger"]),
        // NOTE: keep key "more-likely" (its StableId is referenced by existing moments) — reworded to a
        // would-you-rather choice rather than renamed, so no template row is deleted on migrate.
        new("core", "more-likely", MomentCategory.Fun, "Would You Rather?", "Pick your favourite!", MomentResponseKind.Choice, ["⛰️ Mountains", "🏖️ Beach", "🏙️ City"]),
        new("core", "secret-vote", MomentCategory.Fun, "Secret vote", "Cats or dogs?", MomentResponseKind.Choice, ["🐱 Cats", "🐶 Dogs"]),
        new("core", "emoji-story", MomentCategory.Fun, "Emoji story", "Describe your day using only emojis.", MomentResponseKind.Text),

        new("core", "good-morning", MomentCategory.Voice, "Good morning", "Leave a 20-second morning message.", MomentResponseKind.Voice),
        new("core", "tell-a-story", MomentCategory.Voice, "Tell me something", "Share a story you've never told.", MomentResponseKind.Voice),
        new("core", "best-laugh", MomentCategory.Voice, "Laugh", "Record your best laugh.", MomentResponseKind.Voice),

        new("core", "walk", MomentCategory.Adventure, "Walk", "Walk somewhere you've never been.", MomentResponseKind.Photo),
        new("core", "coffee", MomentCategory.Adventure, "Coffee", "Try a new café.", MomentResponseKind.Photo),
        new("core", "sunset", MomentCategory.Adventure, "Sunset", "Watch today's sunset.", MomentResponseKind.Photo),
        new("core", "heart-shaped", MomentCategory.Adventure, "Find", "Find something heart-shaped outside.", MomentResponseKind.Photo),

        new("core", "best-part", MomentCategory.Reflection, "Best part", "What was today's best moment?", MomentResponseKind.Text),
        new("core", "win", MomentCategory.Reflection, "Win", "What's one thing you're proud of today?", MomentResponseKind.Text),
        new("core", "challenge", MomentCategory.Reflection, "Challenge", "What was difficult today?", MomentResponseKind.Text),
        new("core", "tomorrow", MomentCategory.Reflection, "Tomorrow", "What's one thing you're excited for?", MomentResponseKind.Text),

        new("core", "one-word", MomentCategory.Micro, "One word", "Describe today in one word.", MomentResponseKind.Text),
        new("core", "one-emoji", MomentCategory.Micro, "One emoji", "How was today?", MomentResponseKind.Text),

        // --- Photography pack (Pro) ---
        new("photography", "golden-hour", MomentCategory.Capture, "Golden hour", "Capture the light at golden hour.", MomentResponseKind.Photo),
        new("photography", "leading-lines", MomentCategory.Capture, "Leading lines", "Find leading lines in your surroundings.", MomentResponseKind.Photo),
        new("photography", "reflections", MomentCategory.Capture, "Reflections", "Photograph a reflection.", MomentResponseKind.Photo),
        new("photography", "shadows", MomentCategory.Capture, "Shadows", "Capture an interesting shadow.", MomentResponseKind.Photo),

        // --- Adventure pack (Pro) ---
        new("adventure", "new-street", MomentCategory.Adventure, "New street", "Walk a street you've never walked.", MomentResponseKind.Photo),
        new("adventure", "park", MomentCategory.Adventure, "Park", "Visit a park.", MomentResponseKind.Photo),
        new("adventure", "street-art", MomentCategory.Adventure, "Street art", "Find street art.", MomentResponseKind.Photo),

        // --- Fun pack (Pro) ---
        new("fun", "draw-from-memory", MomentCategory.Draw, "Draw from memory", "Draw each other from memory.", MomentResponseKind.Drawing),
        new("fun", "finish-sentence", MomentCategory.Fun, "Finish the sentence", "Finish: \"The best part of us is…\"", MomentResponseKind.Text),

        // --- Reflection pack (Pro) ---
        new("reflection", "best-decision", MomentCategory.Reflection, "Best decision", "Best decision you made today?", MomentResponseKind.Text),
        new("reflection", "grateful", MomentCategory.Reflection, "Grateful", "Something you're grateful for.", MomentResponseKind.Text),

        // --- Romance pack (Pro) ---
        new("romance", "favourite-memory", MomentCategory.LoveLetter, "Favourite memory", "Share a favourite memory of us.", MomentResponseKind.Text),
        new("romance", "future-letter", MomentCategory.LoveLetter, "Future letter", "Write a short letter to future us.", MomentResponseKind.Text),
        new("romance", "three-compliments", MomentCategory.LoveLetter, "Three compliments", "Three compliments, right now.", MomentResponseKind.Text),

        // --- Garden pack (Pro) ---
        new("garden", "growing", MomentCategory.Capture, "Growing", "Photograph something growing.", MomentResponseKind.Photo),
        new("garden", "favourite-flower", MomentCategory.Capture, "Favourite flower", "Photograph your favourite flower today.", MomentResponseKind.Photo),
        new("garden", "find-a-bee", MomentCategory.Capture, "Find a bee", "Find a bee.", MomentResponseKind.Photo),
    ];

    /// <summary>Default emoji shown for a category when a card has no other glyph.</summary>
    public static string CategoryEmoji(MomentCategory category) => category switch
    {
        MomentCategory.Capture => "📸",
        MomentCategory.Draw => "🎨",
        MomentCategory.LoveLetter => "💌",
        MomentCategory.Voice => "🎤",
        MomentCategory.Fun => "😂",
        MomentCategory.Adventure => "🌿",
        MomentCategory.Reflection => "🧠",
        MomentCategory.Puzzle => "🧩",
        MomentCategory.Micro => "☀️",
        _ => "✨"
    };
}
