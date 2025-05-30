# Conditional Access Policy: Require Compliant Device for Mobile
# Generated from JSON export on 2025-05-30 18:21:01 UTC

# Policy: Require Compliant Device for Mobile
# State: disabled
resource "azuread_conditional_access_policy" "require_compliant_device_for_mobile" {
  display_name = "Require Compliant Device for Mobile"
  state        = "disabled"

  conditions {
    applications {
      included_applications = ["All"]
    }

    users {
      included_users = ["All"]
    }

    client_app_types = ["mobileAppsAndDesktopClients"]

  }

  grant_controls {
    operator          = "OR"
    built_in_controls = ["compliantDevice", "domainJoinedDevice"]
  }

}

