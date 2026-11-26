using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hms.Application.DTOs;
using Hms.Domain.Enums;

namespace Hms.IntegrationTests;

/// <summary>Stock item / price separation, Pharmacy-Admin gating and bulk upload validation.</summary>
public sealed class StockManagementTests(HmsApiFactory factory) : IClassFixture<HmsApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Pharmacy_can_create_update_and_price_items_independently()
    {
        var pharmacy = await ClientAsync("pharmacy@clinic.local");
        var admin = await ClientAsync("admin@clinic.local");
        var hmos = await admin.GetFromJsonAsync<List<HmoOptionDto>>("/api/reference/hmos", Json);
        Assert.NotNull(hmos);
        var hmoId = Assert.Single(hmos!).Id;

        var created = await pharmacy.PostAsJsonAsync("/api/stocks", new CreateStockItemRequest(
            "TEST-ITEM-1", "Test item", StockItemType.Medication, null, null, null, null, "tablet", null, 10m), Json);
        created.EnsureSuccessStatusCode();
        var item = await created.Content.ReadFromJsonAsync<StockItemDto>(Json);
        Assert.NotNull(item);
        Assert.Equal(10m, item!.Quantity);
        Assert.Empty(item.Prices);

        var updated = await pharmacy.PutAsJsonAsync($"/api/stocks/{item.Id}", new UpdateStockItemRequest(
            "TEST-ITEM-1B", "Renamed item", StockItemType.Medication, 25m, true), Json);
        updated.EnsureSuccessStatusCode();
        var renamed = await updated.Content.ReadFromJsonAsync<StockItemDto>(Json);
        Assert.Equal("TEST-ITEM-1B", renamed!.Code);
        Assert.Equal(25m, renamed.Quantity);

        var privatePrice = await pharmacy.PostAsJsonAsync($"/api/stocks/{item.Id}/prices",
            new SetItemPriceRequest(Payers.PrivateId, 750m), Json);
        privatePrice.EnsureSuccessStatusCode();
        var hmoPrice = await pharmacy.PostAsJsonAsync($"/api/stocks/{item.Id}/prices",
            new SetItemPriceRequest(hmoId, 600m), Json);
        hmoPrice.EnsureSuccessStatusCode();
        var priced = await hmoPrice.Content.ReadFromJsonAsync<StockItemDto>(Json);
        Assert.Equal(2, priced!.Prices.Count);
        Assert.Contains(priced.Prices, x => x.PayerId == Payers.PrivateId && x.UnitPrice == 750m);
        Assert.Contains(priced.Prices, x => x.PayerId == hmoId && x.UnitPrice == 600m);

        var filtered = await pharmacy.GetFromJsonAsync<List<StockItemDto>>($"/api/stocks?payerId={Payers.PrivateId}", Json);
        Assert.NotNull(filtered);
        Assert.Contains(filtered!, x => x.Id == item.Id && x.Prices.All(p => p.PayerId == Payers.PrivateId));
    }

    [Fact]
    public async Task Csv_import_reports_row_level_errors_and_imports_valid_rows()
    {
        var pharmacy = await ClientAsync("pharmacy@clinic.local");
        var template = await pharmacy.GetStringAsync("/api/stocks/template?kind=items&format=csv");
        Assert.Contains("code,name,type,quantity", template);

        var csv = "code,name,type,quantity\n"
            + "CSV-ITEM-1,Imported item,Medication,15\n"
            + ",Missing code,Medication,10\n"
            + "CSV-ITEM-2,Bad type,Unknown,10\n"
            + "CSV-ITEM-3,Negative quantity,Test,-1\n";
        var result = await UploadAsync(pharmacy, "/api/stocks/import/items", csv, "items.csv");
        Assert.Equal(4, result.TotalRows);
        Assert.Equal(1, result.Imported);
        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, x => x.Row == 3 && x.Column == "code");
        Assert.Contains(result.Errors, x => x.Row == 4 && x.Column == "type");
        Assert.Contains(result.Errors, x => x.Row == 5 && x.Column == "quantity");

        var items = await pharmacy.GetFromJsonAsync<List<StockItemDto>>("/api/stocks?search=CSV-ITEM", Json);
        Assert.NotNull(items);
        var imported = Assert.Single(items!);
        Assert.Equal(15m, imported.Quantity);
    }

    [Fact]
    public async Task Non_pharmacy_admin_roles_cannot_view_or_manage_stock()
    {
        foreach (var email in new[] { "doctor@clinic.local", "finance@clinic.local", "hmo@clinic.local", "nurse@clinic.local" })
        {
            var client = await ClientAsync(email);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/stocks")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/stocks/payers")).StatusCode);
        }
    }

    private static async Task<ImportResultDto> UploadAsync(HttpClient client, string path, string csv, string fileName)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", fileName);
        var response = await client.PostAsync(path, content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ImportResultDto>(Json))!;
    }

    private async Task<HttpClient> ClientAsync(string email)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "ChangeMe123!"));
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>(Json);
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}
