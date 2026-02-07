using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using BrowserSelector.Models;
using Microsoft.Win32;

namespace BrowserSelector.Services
{
    /// <summary>
    /// Service for detecting Windows Azure AD/M365 accounts and matching them with browser profiles.
    /// </summary>
    public static class WindowsAccountService
    {
        /// <summary>
        /// Gets the primary Windows account (Azure AD joined or Workplace joined).
        /// </summary>
        public static WindowsAccountInfo GetPrimaryAccount()
        {
            // Try Azure AD Join first (most reliable for domain-joined devices)
            var azureAdAccount = GetAzureAdJoinAccount();
            if (azureAdAccount != null)
            {
                azureAdAccount.IsPrimary = true;
                return azureAdAccount;
            }

            // Try Workplace Join (for non-domain devices that are Azure AD registered)
            var workplaceAccount = GetWorkplaceJoinAccount();
            if (workplaceAccount != null)
            {
                workplaceAccount.IsPrimary = true;
                return workplaceAccount;
            }

            // Try Office 365 Identity cache as fallback
            var officeAccount = GetOfficeIdentityAccount();
            if (officeAccount != null)
            {
                officeAccount.IsPrimary = true;
                return officeAccount;
            }

            return null;
        }

        /// <summary>
        /// Gets account from Azure AD Domain Join registry keys.
        /// Location: HKLM\SYSTEM\CurrentControlSet\Control\CloudDomainJoin\JoinInfo\{GUID}
        /// </summary>
        private static WindowsAccountInfo GetAzureAdJoinAccount()
        {
            try
            {
                using var joinInfoKey = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\CloudDomainJoin\JoinInfo");

                if (joinInfoKey == null)
                    return null;

                foreach (var subKeyName in joinInfoKey.GetSubKeyNames())
                {
                    using var subKey = joinInfoKey.OpenSubKey(subKeyName);
                    if (subKey == null) continue;

                    var userEmail = subKey.GetValue("UserEmail") as string;
                    if (string.IsNullOrEmpty(userEmail)) continue;

                    var tenantId = subKey.GetValue("TenantId") as string;

                    // Try to get tenant name from TenantInfo key
                    var tenantName = GetTenantName(tenantId);

                    return new WindowsAccountInfo
                    {
                        TenantId = tenantId ?? string.Empty,
                        TenantName = tenantName,
                        UserEmail = userEmail,
                        Source = AccountSource.AzureAdJoin
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading Azure AD Join registry: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Gets tenant display name from TenantInfo registry key.
        /// </summary>
        private static string GetTenantName(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
                return string.Empty;

            try
            {
                using var tenantInfoKey = Registry.LocalMachine.OpenSubKey(
                    $@"SYSTEM\CurrentControlSet\Control\CloudDomainJoin\TenantInfo\{tenantId}");

                if (tenantInfoKey != null)
                {
                    return tenantInfoKey.GetValue("DisplayName") as string ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading tenant info: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets account from Workplace Join registry (for non-domain-joined devices).
        /// Location: HKU\{SID}\Software\Microsoft\Windows NT\CurrentVersion\WorkplaceJoin\AADNGC
        /// </summary>
        private static WindowsAccountInfo GetWorkplaceJoinAccount()
        {
            try
            {
                // Get current user's SID
                var identity = WindowsIdentity.GetCurrent();
                var userSid = identity?.User?.Value;

                if (string.IsNullOrEmpty(userSid))
                    return null;

                // Try HKU path first
                using var hkuKey = Registry.Users.OpenSubKey(
                    $@"{userSid}\Software\Microsoft\Windows NT\CurrentVersion\WorkplaceJoin\AADNGC");

                if (hkuKey != null)
                {
                    var userId = hkuKey.GetValue("UserID") as string;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        return new WindowsAccountInfo
                        {
                            UserEmail = userId,
                            Source = AccountSource.WorkplaceJoin
                        };
                    }
                }

                // Try HKCU path as fallback
                using var hkcuKey = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows NT\CurrentVersion\WorkplaceJoin\AADNGC");

                if (hkcuKey != null)
                {
                    var userId = hkcuKey.GetValue("UserID") as string;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        return new WindowsAccountInfo
                        {
                            UserEmail = userId,
                            Source = AccountSource.WorkplaceJoin
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading Workplace Join registry: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Gets account from Office 365 Identity cache.
        /// Location: HKCU\Software\Microsoft\Office\16.0\Common\Identity\Identities
        /// </summary>
        private static WindowsAccountInfo GetOfficeIdentityAccount()
        {
            try
            {
                using var identitiesKey = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Office\16.0\Common\Identity\Identities");

                if (identitiesKey == null)
                    return null;

                foreach (var subKeyName in identitiesKey.GetSubKeyNames())
                {
                    // Skip non-ADAL keys
                    if (!subKeyName.Contains("_ADAL"))
                        continue;

                    using var identityKey = identitiesKey.OpenSubKey(subKeyName);
                    if (identityKey == null) continue;

                    var emailAddress = identityKey.GetValue("EmailAddress") as string;
                    if (string.IsNullOrEmpty(emailAddress)) continue;

                    // Skip personal Microsoft accounts for M365 matching
                    if (IsPersonalMicrosoftAccount(emailAddress))
                        continue;

                    var providerId = identityKey.GetValue("ProviderId") as string;

                    return new WindowsAccountInfo
                    {
                        UserEmail = emailAddress,
                        TenantId = providerId ?? string.Empty,
                        Source = AccountSource.OfficeIdentity
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading Office Identity registry: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Checks if an email is a personal Microsoft account (not a work/school account).
        /// </summary>
        private static bool IsPersonalMicrosoftAccount(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            var domain = email.Contains("@")
                ? email.Substring(email.IndexOf('@') + 1).ToLowerInvariant()
                : string.Empty;

            var personalDomains = new[]
            {
                "outlook.com",
                "hotmail.com",
                "live.com",
                "msn.com",
                "gmail.com",
                "yahoo.com",
                "icloud.com"
            };

            return personalDomains.Any(d => domain.Equals(d, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Finds browser profiles that match the given Windows account.
        /// </summary>
        public static List<ProfileMatchResult> FindMatchingProfiles(WindowsAccountInfo account)
        {
            if (account == null)
                return new List<ProfileMatchResult>();

            var results = new List<ProfileMatchResult>();
            var accountDomain = account.Domain;

            if (string.IsNullOrEmpty(accountDomain))
                return results;

            var browsers = BrowserService.GetBrowsersWithColors();

            foreach (var browser in browsers)
            {
                var profiles = BrowserService.GetProfiles(browser);

                foreach (var profile in profiles)
                {
                    var matchResult = EvaluateMatch(account, browser, profile);
                    results.Add(matchResult);
                }
            }

            // Sort: Matching profiles first (by confidence), then non-matching
            return results
                .OrderBy(r => r.Confidence)
                .ThenBy(r => r.Browser.Name)
                .ThenBy(r => r.Profile.Name)
                .ToList();
        }

        /// <summary>
        /// Evaluates the match between a Windows account and a browser profile.
        /// </summary>
        private static ProfileMatchResult EvaluateMatch(
            WindowsAccountInfo account,
            BrowserInfoWithColor browser,
            ProfileInfo profile)
        {
            var result = new ProfileMatchResult
            {
                Account = account,
                Browser = browser,
                Profile = profile,
                Confidence = MatchConfidence.NoMatch,
                MatchReason = "Different domain"
            };

            if (string.IsNullOrEmpty(profile.Email))
            {
                result.MatchReason = "No email in profile";
                return result;
            }

            var profileEmail = profile.Email.ToLowerInvariant();
            var accountEmail = account.UserEmail.ToLowerInvariant();
            var profileDomain = GetDomain(profileEmail);
            var accountDomain = account.Domain;

            // 1. Exact email match
            if (profileEmail.Equals(accountEmail, StringComparison.OrdinalIgnoreCase))
            {
                result.Confidence = MatchConfidence.ExactEmail;
                result.MatchReason = $"Exact match: {profile.Email}";
                return result;
            }

            // 2. Same domain match
            if (profileDomain.Equals(accountDomain, StringComparison.OrdinalIgnoreCase))
            {
                result.Confidence = MatchConfidence.SameDomain;
                result.MatchReason = $"Domain match: @{profileDomain}";
                return result;
            }

            // 3. Subdomain match
            if (profileDomain.EndsWith("." + accountDomain) ||
                accountDomain.EndsWith("." + profileDomain))
            {
                result.Confidence = MatchConfidence.SubdomainMatch;
                result.MatchReason = $"Subdomain: {profileDomain} ~ {accountDomain}";
                return result;
            }

            // No match
            result.MatchReason = $"Different domain: @{profileDomain}";
            return result;
        }

        /// <summary>
        /// Extracts domain from an email address.
        /// </summary>
        private static string GetDomain(string email)
        {
            if (string.IsNullOrEmpty(email))
                return string.Empty;

            var atIndex = email.IndexOf('@');
            return atIndex > 0
                ? email.Substring(atIndex + 1).ToLowerInvariant()
                : string.Empty;
        }
    }
}
