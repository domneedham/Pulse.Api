namespace Pulse.Api.ApiService.Domain;

/// <summary>
/// A Pulse user. The Id matches the Supabase Auth user id (auth.users.id),
/// so the JWT "sub" claim maps directly onto this table.
/// Account deletion tombstones the row (DeletedAt set, identity fields wiped)
/// rather than deleting it, so referencing history stays intact while nothing
/// identifiable remains.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Unique, user-chosen handle (e.g. "domw"). Case-insensitively unique across all users; the
    /// way people find each other (search by username). Chosen at account creation and editable in
    /// profile. Null only briefly before the user has set one.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// IANA timezone id (e.g. "Europe/London"). The app should set it on first run;
    /// defaults to UTC until then.
    /// </summary>
    public string Timezone { get; set; } = "Etc/UTC";

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<UserDevice> Devices { get; set; } = [];
}
