
````sql
//////////////////////////////
//  MySQL DML
/////////////////////////////
AzMyStatus_CL
| where Time_t >= ago(1h)
| where LogicalServerName =~ "{replace to your server name here}"
| where MetricName_s in (
    'Queries',
    'Com_delete', 
    'Com_delete_multi', 
    'Com_insert', 
    'Com_insert_select', 
    'Com_select', 
    'Com_update', 
    'Com_update_multi')         
| project Time_t, MetricName_s, MetricValue_s
| extend Queries_tmp = iff(MetricName_s == 'Queries', toreal(MetricValue_s), 0.0),
            Com_delete_tmp = iff(MetricName_s == 'Com_delete', toreal(MetricValue_s), 0.0),
            Com_delete_multi_tmp = iff(MetricName_s == 'Com_delete_multi', toreal(MetricValue_s), 0.0),
            Com_insert_tmp = iff(MetricName_s == 'Com_insert', toreal(MetricValue_s), 0.0),
            Com_insert_select_tmp = iff(MetricName_s == 'Com_insert_select', toreal(MetricValue_s), 0.0),
            Com_select_tmp = iff(MetricName_s == 'Com_select', toreal(MetricValue_s), 0.0),
            Com_update_tmp = iff(MetricName_s == 'Com_update', toreal(MetricValue_s), 0.0),
            Com_update_multi_tmp = iff(MetricName_s == 'Com_update_multi', toreal(MetricValue_s), 0.0)
| summarize Queries = max(Queries_tmp),
            Com_delete = max(Com_delete_tmp),
            Com_delete_multi = max(Com_delete_multi_tmp),
            Com_insert = max(Com_insert_tmp),
            Com_insert_select = max(Com_insert_select_tmp),
            Com_select = max(Com_select_tmp),
            Com_update = max(Com_update_tmp),
            Com_update_multi = max(Com_update_multi_tmp)
            by Time_t
| order by Time_t asc
| render timechart


//////////////////////////////
//  MySQL IO request
/////////////////////////////
AzMyStatus_CL
| where Time_t >= ago(1h)
| where LogicalServerName =~ "{replace to your server name here}"
| where  MetricName_s  in (
    'Binlog_io_read', 
    'Binlog_io_written', 
    'Binlog_data_read', 
    'Binlog_data_reads',
    'Innodb_data_reads',
    'Innodb_data_read', 
    'Innodb_data_written', 
    'Innodb_data_writes', 
    'Innodb_log_writes', 
    'Innodb_log_written')
| extend Binlog_io_read_tmp = iff(MetricName_s =~ 'Binlog_io_read', toreal(MetricValue_s), 0.0),
         Binlog_io_written_tmp = iff(MetricName_s =~ 'Binlog_io_written', toreal(MetricValue_s), 0.0),
         Innodb_data_read_tmp = iff(MetricName_s =~ 'Innodb_data_read', toreal(MetricValue_s), 0.0),
         Innodb_data_reads_tmp = iff(MetricName_s =~ 'Innodb_data_reads', toreal(MetricValue_s), 0.0),
         Innodb_data_writes_tmp = iff(MetricName_s =~ 'Innodb_data_writes', toreal(MetricValue_s), 0.0),
         Innodb_data_written_tmp = iff(MetricName_s =~ 'Innodb_data_written', toreal(MetricValue_s), 0.0),
         Innodb_log_writes_tmp = iff(MetricName_s =~ 'Innodb_log_writes', toreal(MetricValue_s), 0.0),
         Innodb_log_written_tmp = iff(MetricName_s =~ 'Innodb_log_written', toreal(MetricValue_s), 0.0)
| summarize Binlog_io_read_in_MB = max(Binlog_io_read_tmp)/1024/1024,
            Binlog_io_written_in_MB = max(Binlog_io_written_tmp)/1024/1024,
            Innodb_data_read_in_MB = max(Innodb_data_read_tmp)/1024/1024,
            Innodb_data_reads = max(Innodb_data_reads_tmp),
            Innodb_data_writes = max(Innodb_data_writes_tmp),
            Innodb_data_written_in_MB = max(Innodb_data_written_tmp)/1024/1024,
            Innodb_log_writes = max(Innodb_log_writes_tmp),
            Innodb_log_written_in_MB = max(Innodb_log_written_tmp)/1024/1024
            by Time_t
| order by Time_t asc
| render timechart


//////////////////////////////
//  MySQL temp table usage
/////////////////////////////
AzMyStatus_CL
| where Time_t >= ago(1h)
| where LogicalServerName =~ "{replace to your server name here}"
| where MetricName_s in (
    'Created_tmp_disk_tables', 
    'Created_tmp_files', 
    'Created_tmp_tables')
| project Time_t, MetricName_s, MetricValue_s
| extend Created_tmp_disk_tables_tmp = iff(MetricName_s == 'Created_tmp_disk_tables', toreal(MetricValue_s), 0.0), 
         Created_tmp_files_tmp = iff(MetricName_s == 'Created_tmp_files', toreal(MetricValue_s), 0.0),
         Created_tmp_tables_tmp = iff(MetricName_s == 'Created_tmp_tables', toreal(MetricValue_s), 0.0)
| summarize Created_tmp_disk_tables = max(Created_tmp_disk_tables_tmp),
            Created_tmp_files = max(Created_tmp_files_tmp),
            Created_tmp_tables = max(Created_tmp_tables_tmp) by Time_t
| order by Time_t asc
| render timechart 


//////////////////////////////
//  MySQL buffer pool usage
/////////////////////////////
AzMyStatus_CL
| where Time_t >= ago(1h)
| where LogicalServerName =~ "{replace to your server name here}"
| where MetricName_s in (
    'Innodb_buffer_pool_read_requests', 
    'Innodb_buffer_pool_reads', 
    'Innodb_buffer_pool_write_requests', 
    'Innodb_buffer_pool_pages_dirty', 
    'Innodb_buffer_pool_wait_free')
| project Time_t, MetricName_s, MetricValue_s
| extend Innodb_buffer_pool_read_requests_tmp = iff(MetricName_s == 'Innodb_buffer_pool_read_requests', toreal(MetricValue_s), 0.0), 
         Innodb_buffer_pool_reads_tmp = iff(MetricName_s == 'Innodb_buffer_pool_reads', toreal(MetricValue_s), 0.0),
         Innodb_buffer_pool_write_requests_tmp = iff(MetricName_s == 'Innodb_buffer_pool_write_requests', toreal(MetricValue_s), 0.0),
         Innodb_buffer_pool_pages_dirty_tmp = iff(MetricName_s == 'Innodb_buffer_pool_pages_dirty', toreal(MetricValue_s), 0.0),
         Innodb_buffer_pool_wait_free_tmp = iff(MetricName_s == 'Innodb_buffer_pool_wait_free', toreal(MetricValue_s), 0.0)
| summarize Innodb_buffer_pool_read_requests = max(Innodb_buffer_pool_read_requests_tmp), 
            Innodb_buffer_pool_reads = max(Innodb_buffer_pool_reads_tmp),
            Innodb_buffer_pool_write_requests = max(Innodb_buffer_pool_write_requests_tmp),
            Innodb_buffer_pool_pages_dirty = max(Innodb_buffer_pool_pages_dirty_tmp),
            Innodb_buffer_pool_wait_free = max(Innodb_buffer_pool_wait_free_tmp) by Time_t
| extend Innodb_buffer_pool_reads_cache_hit_percentage = round((Innodb_buffer_pool_read_requests-Innodb_buffer_pool_reads)*100/Innodb_buffer_pool_read_requests,3)
| order by Time_t asc
| render timechart 
````