# Conditional Access Policy: Require MFA for All Users
# Generated from JSON export on 2025-05-30 18:21:01 UTC

# Policy: Require MFA for All Users
# State: enabled
resource "azuread_conditional_access_policy" "require_mfa_for_all_users" {
  display_name = "Require MFA for All Users"
  state        = "enabled"

  conditions {
    client_app_types = ["browser", "mobileAppsAndDesktopClients"]

  }

  grant_controls {
    operator          = "OR"
    built_in_controls = ["mfa"]
  }

}

