using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Text.Json;
using TechTalk.SpecFlow;
using BlackKiteApiLib.Clients;
using RestSharp;

namespace BlackKiteApiTests.StepDefinitions
{
    [Binding]
    public class BlackKiteSteps
    {
        private readonly BlackKiteClient _client = new();
        private int _createdEcosystemId;
        private int _createdCompanyId;
        private int _notificationId;
        private long _findingId;
        private string _moduleName = string.Empty;
        private long _scenarioFindingId;

        

    [Given("Authenticate to Black Kite API")]
    public async Task Authenticate()
    {
        await _client.AuthenticateAsync("fb7af9ea201a4c4aaa69b8b6eed09816", "323814eae1924352b00c1aaacc3c21f6");
    }

    [When("Create a new ecosystem")]
    public async Task CreateNewEcosystem()
    {
        var request = new RestRequest("/api/v2/ecosystems", Method.Post);
        _client.AddAuthHeader(request);
        request.AddJsonBody(new
        {
            Name = $"TestEcoFurkanAuto_{Guid.NewGuid()}"
        });

        var response = await _client.ExecuteAsync(request);
        Console.WriteLine($"[DEBUG] Ecosystem response: {response.Content}");
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(response.Content!);
        _createdEcosystemId = int.Parse(result["EcosystemId"].ToString()!);

        Assert.IsTrue(response.IsSuccessful);
    }

    [Then("Verified that ecosystem is created")]
    public async Task VerifyEcosystemIsCreated()
    {
        Console.WriteLine($"[STEP] Verifying that ecosystem ID {_createdEcosystemId} exists...");

        var request = new RestRequest($"/api/v2/ecosystems/{_createdEcosystemId}", Method.Get);
        _client.AddAuthHeader(request);

        var response = await _client.ExecuteAsync(request);
        Console.WriteLine($"[DEBUG] Ecosystem GET Response: {response.StatusCode} - {response.Content}");

        Assert.IsTrue(response.IsSuccessful, "Ecosystem retrieval failed.");

        var json = JsonDocument.Parse(response.Content!);
        int returnedId = json.RootElement.GetProperty("EcosystemId").GetInt32();

        Console.WriteLine($"[INFO] Retrieved EcosystemId: {returnedId}");

        Assert.AreEqual(_createdEcosystemId, returnedId, "Returned EcosystemId does not match the created one.");
    }

    [When(@"Create a new company with domain ""(.*)""")]
    public async Task CreateNewCompany(string domain)
    {
        var request = new RestRequest("/api/v2/companies", Method.Post);
        _client.AddAuthHeader(request);
        request.AddJsonBody(new
        {
            MainDomainValue = domain,
            EcosystemId = _createdEcosystemId,
            LicenseType = "ContinuousAnnual",
            IsSubsidiary = false,
            IsCloudProvider = false
        });

        var response = await _client.ExecuteAsync(request);
        Console.WriteLine($"[DEBUG] Company creation response: {response.Content}");
        Assert.IsTrue(response.IsSuccessful, "Company creation failed!");

        var result = JsonDocument.Parse(response.Content!);
        var root = result.RootElement;

        _createdCompanyId = root.GetProperty("CompanyId").GetInt32();
        Console.WriteLine($"[DEBUG] Created company id: {_createdCompanyId}");

        var domainValue = root.GetProperty("MainDomainValue").GetString();
        Assert.AreEqual(domain, domainValue, "Returned domain does not match the requested one.");
    }

    [Then(@"Verified that scan status ""(.*)""")]
    public async Task VerifyScanStatus(string expectedStatus)
    {
        string currentStatus = "";
        int retries = 0;
        
        // burada ScanStatus: "Extended Rescan Results Ready" değerinin kontrolü sağlanıyor.
        // olası bir uzun sürme durumuna karşılık bir while döngüsü eklendi ve 2 dakika 20 saniye max timeout belirlendi.
        while (currentStatus != expectedStatus && retries < 20)
        {
            var request = new RestRequest($"/api/v2/companies/{_createdCompanyId}", Method.Get);
            _client.AddAuthHeader(request);
            var response = await _client.ExecuteAsync(request);
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(response.Content!);
            currentStatus = result["ScanStatus"].ToString()!;
            Console.WriteLine($"ScanStatus: {currentStatus}");

            if (currentStatus != expectedStatus)
            {
                await Task.Delay(10000); // 10 saniye bekle
                retries++;
            }
        }

        Assert.AreEqual(expectedStatus, currentStatus);
    }

    [When(@"Get notifications for company ""(.*)""")]
    public async Task GetNotificationsForCompany(string expectedCompanyName)
    {
        Console.WriteLine("[STEP] Getting notifications...");

        var request = new RestRequest("/api/v2/notifications", Method.Get);
        _client.AddAuthHeader(request);
        request.AddParameter("page_number", 1);
        request.AddParameter("page_size", 10);
        request.AddParameter("companyId", _createdCompanyId);

        var response = await _client.ExecuteAsync(request);
        Console.WriteLine($"[DEBUG] Notifications Response: {response.Content}");
        Assert.IsTrue(response.IsSuccessful);

        var allNotifications = JsonSerializer.Deserialize<List<JsonElement>>(response.Content!);
        Assert.IsNotEmpty(allNotifications, "No notifications returned from API.");

        // Sadece NotificationType == "findings" olanları filtrele
        var findingsNotifications = allNotifications
            .Where(n => n.GetProperty("NotificationType").GetString() == "findings")
            .ToList();

        Assert.IsNotEmpty(findingsNotifications, "No 'findings' type notifications found.");

        // Listeye atılan her bir notification için CompanyId ve Company name kontrolü
        foreach (var notif in findingsNotifications)
        {
            int companyId = notif.GetProperty("CompanyId").GetInt32();
            string companyName = notif.GetProperty("Company").GetString()!;
            Console.WriteLine($"[CHECK] Notification - CompanyId: {companyId}, Company: {companyName}, Type: findings");

            Assert.AreEqual(_createdCompanyId, companyId);
            StringAssert.AreEqualIgnoringCase(expectedCompanyName, companyName);
        }

        // Random NotificationId seç (sadece findings tipi içinden)
        var randomNotif = findingsNotifications[new Random().Next(findingsNotifications.Count)];
        _notificationId = randomNotif.GetProperty("NotificationId").GetInt32();
        Console.WriteLine($"[SELECTED] Random NotificationId: {_notificationId}");
    }


    [Then("All notifications should match the company id and name")]
    public void AllNotificationsShouldMatch()
    {
        // Doğrulama bir önceki stepde when ile gerçekleştirildi.
        // BDD yazımının bozunmaması adına burası eklendi.
    }

    [When("Get findings for a random notification")]
    public async Task GetFindingsForRandomNotification()
    {
        Console.WriteLine("[STEP] Getting findings for selected notification...");
        var request = new RestRequest($"/api/v2/notifications/{_notificationId}/findings", Method.Get);
        _client.AddAuthHeader(request);
        request.AddParameter("page_number", 1);
        request.AddParameter("page_size", 10);

        var response = await _client.ExecuteAsync(request);
        Console.WriteLine($"[DEBUG] Findings Response: {response.Content}");
        Assert.IsTrue(response.IsSuccessful);

        var findings = JsonSerializer.Deserialize<List<JsonElement>>(response.Content!);
        Assert.IsNotEmpty(findings);

        var randomFinding = findings[new Random().Next(findings.Count)];
        _findingId = randomFinding.GetProperty("FindingId").GetInt64();
        _moduleName = randomFinding.GetProperty("Module").GetString()!;

        Console.WriteLine($"[SELECTED] FindingId: {_findingId}, Module: {_moduleName}");
    }

    [Then("Findings should not be empty")]
    public void FindingsShouldNotBeEmpty()
    {
        // Doğrulama bir önceki stepde when ile gerçekleştirildi.
        // BDD yazımının bozunmaması adına burası eklendi.
    }

    [When("Get finding detail for selected finding")]
    public async Task GetFindingDetail()
    {
        Console.WriteLine("[STEP] Getting finding detail...");

        // Bu adımda finding id ile gelen module namede harfler lowercase yapıldı ve bosluklar silindi (ek olarak özel karakterlerde)
        string normalizedModule = new string(_moduleName
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .ToLower();

        Console.WriteLine($"[INFO] Normalized module: {normalizedModule}");

        // Bu kısımda random seçilen findingin module nameine uygun olan api servisine gidilir.
        var request = new RestRequest($"/api/v2/companies/{_createdCompanyId}/findings/{normalizedModule}/{_findingId}", Method.Get);
        _client.AddAuthHeader(request);

        var response = await _client.ExecuteAsync(request);
        Console.WriteLine($"[DEBUG] Finding Detail Response: {response.Content}");
        Assert.IsTrue(response.IsSuccessful);

        var json = JsonDocument.Parse(response.Content!);
        var returnedFindingId = json.RootElement.GetProperty("FindingId").GetInt64();

        _scenarioFindingId = returnedFindingId;

        Console.WriteLine($"[RESULT] Returned FindingId: {_scenarioFindingId}");
    }

    [Then("Verify that finding id is match with response")]
    public void FindingIdShouldMatch()
    {
        Assert.AreEqual(_findingId, _scenarioFindingId);
    }

    [When("Update selected finding status")]
    public async Task UpdateFindingStatus()
    {
        Console.WriteLine("[STEP] Checking current finding status before attempting update...");

        // Önce current status'u al
        string normalizedModule = new string(_moduleName
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .ToLower();

        var getRequest = new RestRequest($"/api/v2/companies/{_createdCompanyId}/findings/{normalizedModule}/{_findingId}", Method.Get);
        _client.AddAuthHeader(getRequest);

        var getResponse = await _client.ExecuteAsync(getRequest);
        Console.WriteLine($"[DEBUG] Current Finding Detail Response: {getResponse.Content}");
        Assert.IsTrue(getResponse.IsSuccessful);

        var findingJson = JsonDocument.Parse(getResponse.Content!);
        string currentStatus = findingJson.RootElement.GetProperty("Status").GetString()!;
        Console.WriteLine($"[INFO] Current status of finding {_findingId}: {currentStatus}");

        // Status zaten Remediated ise Acknowledged statüsünü gönderir.
        string newStatus = currentStatus.Equals("Remediated", StringComparison.OrdinalIgnoreCase)
            ? "Acknowledged"
            : "Remediated";

        Console.WriteLine($"[STEP] Updating status to '{newStatus}'...");

        var patchRequest = new RestRequest($"/api/v2/companies/{_createdCompanyId}/findings/{_findingId}", Method.Patch);
        _client.AddAuthHeader(patchRequest);
        patchRequest.AddHeader("Content-Type", "application/json");

        patchRequest.AddJsonBody(new
        {
            Status = newStatus,
            Comment = "FurkanAutomationTest"
        });

        var patchResponse = await _client.ExecuteAsync(patchRequest);
        Console.WriteLine($"[DEBUG] PATCH Response: {patchResponse.StatusCode} - {patchResponse.Content}");

        Assert.IsTrue(patchResponse.IsSuccessful, $"Status update to '{newStatus}' failed.");
    }


    [Then("Verify that finding status update action is logged")]
    public async Task VerifyFindingStatusUpdateShouldBeLogged()
    {
        Console.WriteLine("[STEP] Verifying finding status update log...");

        var request = new RestRequest("/api/v2/log/company", Method.Get);
        _client.AddAuthHeader(request);
        request.AddParameter("id", _createdCompanyId);
        request.AddParameter("date_range", "LastWeek");
        request.AddParameter("query", _findingId);

        var response = await _client.ExecuteAsync(request);
        Console.WriteLine($"[DEBUG] Log Response: {response.Content}");
        Assert.IsTrue(response.IsSuccessful);

        var logs = JsonSerializer.Deserialize<List<JsonElement>>(response.Content!);
        Assert.IsNotEmpty(logs, "No logs found for the company.");

        // Aranan logu bul
        var matchingLog = logs.FirstOrDefault(log =>
            log.GetProperty("LogType").GetString() == "Finding Status Changed" &&
            log.GetProperty("InsertUser").GetString() == "API" &&
            log.GetProperty("Description").GetString()!.Contains(_findingId.ToString()));

        Console.WriteLine(matchingLog.ValueKind != JsonValueKind.Undefined
            ? $"[FOUND] Log matched for FindingId: {_findingId}"
            : $"[NOT FOUND] No matching log entry found for FindingId: {_findingId}");

        Assert.AreNotEqual(JsonValueKind.Undefined, matchingLog.ValueKind, "No matching log entry found.");
    }

    [When("Delete the created company")]
    public async Task DeleteTheCreatedCompany()
    {
        Console.WriteLine($"[STEP] Deleting company with ID: {_createdCompanyId} in Ecosystem: {_createdEcosystemId}");

        var request = new RestRequest($"/api/v2/companies/{_createdCompanyId}", Method.Delete);
        _client.AddAuthHeader(request);
        request.AddParameter("EcosystemId", _createdEcosystemId);

        var response = await _client.ExecuteAsync(request);
        Console.WriteLine($"[DEBUG] Delete Company Response: {response.StatusCode}");

        Assert.IsTrue(response.IsSuccessful, "Company delete request failed.");
    }

    [Then("Verify that the company is deleted from created ecosystem")]
    public async Task VerifyDeleteCompanyFromCreatedEcosystem()
    {
        Console.WriteLine($"[STEP] Verifying that company ID {_createdCompanyId} is not part of ecosystem {_createdEcosystemId}...");

        var request = new RestRequest($"/api/v2/companies/{_createdCompanyId}", Method.Get);
        _client.AddAuthHeader(request);

        var response = await _client.ExecuteAsync(request);
        Console.WriteLine($"[DEBUG] Company GET Response: {response.StatusCode} - {response.Content}");

        Assert.IsTrue(response.IsSuccessful, "Failed to get company details.");

        var json = JsonDocument.Parse(response.Content!);
        var ecosystems = json.RootElement.GetProperty("Ecosystems");

        bool ecosystemExists = ecosystems.EnumerateArray()
            .Any(e => e.GetProperty("EcosystemId").GetInt32() == _createdEcosystemId);

        Console.WriteLine(ecosystemExists
            ? $"[FAIL] Company is still part of ecosystem {_createdEcosystemId}."
            : $"[PASS] Company is NOT part of ecosystem {_createdEcosystemId}.");

        Assert.IsFalse(ecosystemExists, $"Company is still linked to ecosystem {_createdEcosystemId}.");
    }


    [When("Delete the created ecosystem")]
    public async Task DeleteTheCreatedEcosystem()
    {
        Console.WriteLine($"[STEP] Deleting ecosystem with ID: {_createdEcosystemId}");

        var request = new RestRequest($"/api/v2/ecosystems/{_createdEcosystemId}", Method.Delete);
        _client.AddAuthHeader(request);

        var response = await _client.ExecuteAsync(request);
        Console.WriteLine($"[DEBUG] Delete Ecosystem Response: {response.StatusCode}");

        Assert.IsTrue(response.IsSuccessful, "Ecosystem delete request failed.");
    }

    [Then("The ecosystem should no longer exist")]
    public async Task EcosystemShouldNotExist()
    {
        Console.WriteLine($"[STEP] Verifying that ecosystem ID {_createdEcosystemId} is deleted...");

        var request = new RestRequest($"/api/v2/ecosystems/{_createdEcosystemId}", Method.Get);
        _client.AddAuthHeader(request);

        var response = await _client.ExecuteAsync(request);
        Console.WriteLine($"[DEBUG] Ecosystem GET Response: {response.StatusCode} - {response.Content}");

        Assert.AreEqual(404, (int)response.StatusCode);
        Assert.IsTrue(response.Content!.Contains("Ecosystem not found"));
    }

}
}