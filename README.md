# Ingest Mysql Metrics from performance_schema.global_status into the Azure Monitor Log Analytics Workspace

MySQL performance_schema.global_status have rich internal functioning metrics, and the stored metrics are the snapshot of a particular point of time when you select the metric table. When troubleshooting the problem, we need to review and accumulate the historical metrics data with powerful query functions like [Azure Monitor Kusto Queries](https://docs.microsoft.com/en-us/azure/azure-monitor/log-query/query-language) to help understand the overall status. In this tool, it will introduce how to post the metrics to Azure Monitor Log Analytics Workspace and leverage the powerful Kusto query language to monitor the MySQL statistics metrics.

## Ingest the Metrics to external monitoring tool – Azure Monitor:
1.  We need to run the ingestion code side by side on the same VM. The ingestion sample code will query the MySQL performance_schema.global_status metrics then post the data to the Logical Workspace in a regular 30-sec interval.
2. Provision a [Log Analytics Workspace](https://docs.microsoft.com/en-us/azure/azure-monitor/learn/quick-create-workspace) to store the posted metrics. The Ingestion sample code performs POST Azure Monitor custom log through HTTP REST API: [Link](https://docs.microsoft.com/en-us/azure/azure-monitor/platform/data-collector-api)
3. The ingestion sample code is developed with .NET Core 3.1, and you could check out from the [GitHub repo](https://github.com/ShawnXxy/AzMy-Metrics-Bin.git)

## Detail usage instructions about the sample ingesting code:
1. Install .NET Core on the Linux VM where ProxySQL is located. Refer to https://docs.microsoft.com/dotnet/core/install/linux-package-manager-ubuntu-1804
    ```bash
    wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    sudo add-apt-repository universe
    sudo apt-get update
    sudo apt-get install apt-transport-https
    sudo apt-get update
    sudo apt-get install dotnet-sdk-6.0
    ```
2. Get the Custom ID and Shared Key of the Log Analytics Workspace
    ```text
    1)	In the Azure portal, locate your Log Analytics workspace.
    2)	Select Advanced Settings and then Connected Sources.
    3)	To the right of Workspace ID, select the copy icon, and then paste the ID as the value of the Customer ID input for the sample application input.
    4)	To the right of Primary Key, select the copy icon, and then paste the ID as the value of the Shared Key input for the sample application input.
    ```
3. Checkout the sample code and run:
    ```bash
    git clone https://github.com/ShawnXxy/AzMy-Metrics-Bin.git
    cd AzMy-Metrics-Bin
    dotnet build
    sudo dotnet run
    ```

    Here are some details about the sample:
    - It is a console application which will ask for the input of the connection string for MySQL, (Log Workspace) custom ID and Shared key.
    - The sample currently register a 30-sec timer to periodically access the MySQL performance_schema.global_status tables through MySQL protocol and post data into the Log Analytics Workspace
    - The global_status table name would be used as the Custom Log Type Name, and the Log Analytics will automatically add _CL suffix to generate the complete Custom Log Type Name. For example, the stats table stats_memory_metrics will become stats_memroy_metrics_CL in the Custom Logs list. 

4. Use Kusto query in Log Analytics Workspace to operate the MySQL performance_schema.global_status metrics data.

>Disclaimer: This sample code is available AS IS with no warranties and support from Microsoft. Please raise an issue in Github if you encounter any issues and I will try our best to address it.











