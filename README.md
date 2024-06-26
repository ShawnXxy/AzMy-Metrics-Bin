# Ingest Mysql Metrics from information_schema.global_status into the Azure Monitor Log Analytics Workspace

MySQL information_schema.global_status have rich internal functioning metrics, and the stored metrics are the snapshot of a particular point of time when you select the metric table. When troubleshooting the problem, we need to review and accumulate the historical metrics data with powerful query functions like [Azure Monitor Kusto Queries](https://docs.microsoft.com/en-us/azure/azure-monitor/log-query/query-language) to help understand the overall status. In this tool, it will introduce how to post the metrics to Azure Monitor Log Analytics Workspace and leverage the powerful Kusto query language to monitor the MySQL statistics metrics.

Here are some details about the tool:
1. It is a console application which will ask for the input of the connection string for MySQL, (Log Workspace) custom ID and Shared key. 
2. We need to run the ingestion code side by side in a VM that is allowed to connected to the target MySQL. The ingestion sample code will query the MySQL information_schema.global_status metrics and then post the data to the Logical Workspace in a regular 30-sec interval.
3. Data stored in Azure Log Analytics and data retention can be controlled based on preference. Details can be referred at [Configure the default workspace retention policy](https://learn.microsoft.com/en-us/azure/azure-monitor/logs/data-retention-archive?tabs=portal-1%2Cportal-2)

Below is a sample turnout that you can monitor workload like amount of DML, DDL, buffer pool usage, data read/write, etc. that could be useful when inestigating performance or usage.
Further, you can leverage Azure Monitor to subscribe alert based on preference: https://docs.microsoft.com/en-us/azure/azure-monitor/alerts/tutorial-log-alert
![image](https://user-images.githubusercontent.com/17153057/188049380-867e90b2-5e2d-4ae4-a2b9-ad3c247f71e7.png)

## How does this work
This tool is running as the same logic described at https://techcommunity.microsoft.com/t5/azure-database-for-mysql-blog/monitor-mysql-server-performance-using-performance-schema-and/ba-p/2001819.
To make the process explained in the above blog easier, this tool automatically collected data and upload to Log Analyticas Workspace.
The Ingestion sample code performs POST Azure Monitor custom log through HTTP REST API: [Link](https://docs.microsoft.com/en-us/azure/azure-monitor/platform/data-collector-api).


## Prerequisite
- Provision a [Log Analytics Workspace](https://docs.microsoft.com/en-us/azure/azure-monitor/learn/quick-create-workspace) to store the posted metrics. 
- The ingestion sample code is developed with .NET Core 6.0. Install .NET Core on a Linux VM where is allowed to connected to the target MySQL. Refer to https://docs.microsoft.com/dotnet/core/install/linux-package-manager-ubuntu-1804
  
    ```bash
    wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    sudo add-apt-repository universe
    sudo apt-get update
    sudo apt-get install apt-transport-https
    sudo apt-get update
    sudo apt-get install dotnet-sdk-6.0
    ```
    If you hit error like "A fatal error occurred. The folder [/usr/share/dotnet/host/fxr] does not exist", it is a result of mixed packages.
    This `dotnet` packages are part of the Ubuntu repo, and they are conflicting with the Microsoft packages.  Please refer to https://stackoverflow.com/questions/74174560/dotnet-sdk-not-installing-in-ubuntu-22-04 to fix it.
    
- For MySQL major version lower than 8, please making sure parameter ***show_compatibility_56*** is enabled. It should be OFF by default, you can check it and modify it in Azure Portal
    ![image](https://user-images.githubusercontent.com/17153057/188105338-3c0bede3-512e-439f-b16a-41ae14bf1670.png)


## Detail usage instructions about the sample ingesting code:
1. Checkout the sample code and run:
    ```bash
    git clone https://github.com/ShawnXxy/AzMy-Metrics-Bin.git
    cd AzMy-Metrics-Bin
    dotnet build
    sudo dotnet run
    ```
   
2. Target MYSQL host, username, and password will be asked at prompt. Besides, navigate to Log Analytics Workspace and find the needed Workspace ID and Key shown as below: 
    ```text
    1)	In the Azure portal, locate your Log Analytics workspace.
    2)	Select Advanced Settings and then Connected Sources.
    3)	To the right of Workspace ID, select the copy icon, and then paste the ID as the value of the Customer ID input for the sample application input.
    4)	To the right of Primary Key, select the copy icon, and then paste the ID as the value of the Shared Key input for the sample application input.
    ```  
  
    ![image](https://user-images.githubusercontent.com/17153057/185856549-c74cee3a-9e97-4f51-b072-074a6511b9f3.png)

    A success output could be like below:

    ![image](https://user-images.githubusercontent.com/17153057/188066376-21807748-049f-4f28-94e2-aed8f36f3627.png)
   
3. Use [Kusto query](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/query/) in Log Analytics Workspace to operate the MySQL information_schema.global_status metrics data. The global_status table name would be used as the Custom Log Type Name, and the Log Analytics will automatically add _CL suffix to generate the complete Custom Log Type Name. For example, the  table global_status will become global_status_CL in the Custom Logs list. 
    ![image](https://user-images.githubusercontent.com/17153057/188055029-ad604272-3709-4ccc-b9c6-70a02cdf8db3.png)

## Sample Kusto Query 
[This](/Use%20Case%20and%20Sample%20Kusto%20Query.md) summarized some sample Kusto queries to monitor the MySQL statistics metrics.

## Limitation
* It has be run in a LInux machine that is allowed to connected to the target MySQL.
* It does not add support for the log rotation monitoring by FileSystemWatcher. The saved output would constantly grow for current version.
* When VM restarted or targeted MySQL server restarted, or any reasons the connections lost to MySQL, the ingestion sample code will need to be restarted manually.


---


>Disclaimer: This sample code is available AS IS with no warranties and support from Microsoft. Please raise an issue in Github if you encounter any issues and I will try our best to address it.











