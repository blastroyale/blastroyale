using System;
using System.Collections.Generic;
using System.Globalization;

public class SingularAdData : Dictionary<string, object> {

    public static class AdPlatforms {
        public static readonly string MOPUB = "mopub";
    }

    // Admon
    private const string ADMON_IS_ADMON_REVENUE = "is_admon_revenue";
    private const string ADMON_AD_PLATFORM = "ad_platform";
    private const string ADMON_CURRENCY = "ad_currency";
    private const string ADMON_REVENUE = "ad_revenue";
    private const string ADMON_NETWORK_NAME = "ad_mediation_platform";
    private const string ADMON_AD_TYPE = "ad_type";
    private const string ADMON_AD_GROUP_TYPE = "ad_group_type";
    private const string ADMON_IMPRESSION_ID = "ad_impression_id";
    private const string ADMON_AD_PLACEMENT_NAME = "ad_placement_name";
    private const string ADMON_AD_UNIT_ID = "ad_unit_id";
    private const string ADMON_AD_UNIT_NAME = "ad_unit_name";
    private const string ADMON_AD_GROUP_ID = "ad_group_id";
    private const string ADMON_AD_GROUP_NAME = "ad_group_name";
    private const string ADMON_AD_GROUP_PRIORITY = "ad_group_priority";
    private const string ADMON_PRECISION = "ad_precision";
    private const string ADMON_PLACEMENT_ID = "ad_placement_id";
    private const string IS_REVENUE_EVENT_KEY = "is_revenue_event";
    private const string REVENUE_AMOUNT_KEY = "r";
    private const string REVENUE_CURRENCY_KEY = "pcc";

    private readonly string[] RequiredParams = new string[]{
            ADMON_AD_PLATFORM,
            ADMON_CURRENCY,
            ADMON_REVENUE};

    public SingularAdData(string adPlatform, string currency, double revenue) {
        SetValue(ADMON_AD_PLATFORM, adPlatform);
        SetValue(ADMON_CURRENCY, currency);
        SetValue(REVENUE_CURRENCY_KEY, currency);

        // Enforcing decimal separator as Dot "." . Because different regions have different decimal serparators e.g. Comma, Space
        SetValue(ADMON_REVENUE, revenue);
        SetValue(REVENUE_AMOUNT_KEY, revenue);

        SetValue(ADMON_IS_ADMON_REVENUE, true);
        SetValue(IS_REVENUE_EVENT_KEY, true);

        // by default the network name is the adPlatform (except for mediation)
        SetValue(ADMON_NETWORK_NAME, adPlatform);
    }


    public SingularAdData WithNetworkName(string networkName) {
        SetValue(ADMON_NETWORK_NAME, networkName);
        return this;
    }

    public SingularAdData WithAdType(string adType) {
        SetValue(ADMON_AD_TYPE, adType);
        return this;
    }

    public SingularAdData WithAdGroupType(string adGroupType) {
        SetValue(ADMON_AD_GROUP_TYPE, adGroupType);
        return this;
    }

    public SingularAdData WithImpressionId(string impressionId) {
        SetValue(ADMON_IMPRESSION_ID, impressionId);
        return this;
    }

    public SingularAdData WithAdPlacmentName(string adPlacementName) {
        SetValue(ADMON_AD_PLACEMENT_NAME, adPlacementName);
        return this;
    }

    public SingularAdData WithAdUnitId(string adUnitId) {
        SetValue(ADMON_AD_UNIT_ID, adUnitId);
        return this;
    }

    public SingularAdData WithAdUnitName(string adUnitName) {
        SetValue(ADMON_AD_UNIT_NAME, adUnitName);
        return this;
    }

    public SingularAdData WithAdGroupId(string adGroupId) {
        SetValue(ADMON_AD_GROUP_ID, adGroupId);
        return this;
    }

    public SingularAdData WithAdGroupName(string adGroupName) {
        SetValue(ADMON_AD_GROUP_NAME, adGroupName);
        return this;
    }

    public SingularAdData WithAdGroupPriority(string adGroupPriority) {
        SetValue(ADMON_AD_GROUP_PRIORITY, adGroupPriority);
        return this;
    }

    public SingularAdData WithPrecision(string precision) {
        SetValue(ADMON_PRECISION, precision);
        return this;
    }

    public SingularAdData WithPlacementId(string placementId) {
        SetValue(ADMON_PLACEMENT_ID, placementId);
        return this;
    }

    private void SetValue(string key, object value) {
        try {
            if (value == null || value.ToString().Trim() == string.Empty) {
                return;
            }

            this[key] = value;
        } catch (Exception) { }
    }

    public bool HasRequiredParams() {
        foreach (var key in RequiredParams) {
            if (!ContainsKey(key) || this[key] == null || this[key].ToString().Trim() == string.Empty) {
                return false;
            }
        }

        return true;
    }
}
