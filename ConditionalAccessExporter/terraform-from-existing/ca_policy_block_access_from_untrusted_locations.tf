# Conditional Access Policy: Block Access from Untrusted Locations
# Generated from JSON export on 2025-05-30 18:21:01 UTC

# Policy: Block Access from Untrusted Locations
# State: enabled
resource "azuread_conditional_access_policy" "block_access_from_untrusted_locations" {
  display_name = "Block Access from Untrusted Locations"
  state        = "enabled"

  conditions {
    applications {
      included_applications = ["All"]
    }

    users {
      included_users = ["All"]
    }

    client_app_types = ["browser", "mobileAppsAndDesktopClients"]

  }

  grant_controls {
    operator          = "OR"
    built_in_controls = ["block"]
  }

}

