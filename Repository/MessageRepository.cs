using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SankalpAPI.Helper;
using SankalpAPI.Interface;
using SankalpAPI.Model;

namespace SankalpAPI.Repository
{
    public class MessageRepository : IMessageRepository
    {
        private readonly clsDataAccess _dataAccess;

        public MessageRepository(clsDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public Task<List<PersonalizedMessageDto>> GetUnsyncedMessagesAndMarkSyncedAsync(string DeviceID, long? portalId = null, long? msg_id = null)
        {
            var list = new List<PersonalizedMessageDto>();

            SqlParameter[] parameters =
            {
                new SqlParameter("@p_device_id", SqlDbType.NVarChar, 255) { Value = DeviceID ?? string.Empty },
                new SqlParameter("@p_portal_id", (portalId.HasValue && portalId.Value > 0) ? (object)portalId.Value : DBNull.Value),
                new SqlParameter("@p_msg_id", (msg_id.HasValue && msg_id.Value > 0) ? (object)msg_id.Value : DBNull.Value)
            };

            DataTable dt = _dataAccess.GetDataTableWithProc("sp_get_unsynced_messages", parameters);

            if (dt.Rows.Count == 0)
            {
                return Task.FromResult(list);
            }

            var logIds = new List<long>();

            foreach (DataRow row in dt.Rows)
            {
                long logId = SafeLong(row, "msg_log_id");
                if (logId > 0)
                {
                    logIds.Add(logId);
                }

                list.Add(new PersonalizedMessageDto
                {
                    msg_id = SafeLong(row, "msg_id"),
                    msg_log_id = logId > 0 ? logId : null,
                    DeviceID = SafeString(row, "device_id") ?? DeviceID ?? string.Empty,
                    PortalId = SafeNullableLong(row, "portal_id"),
                    PortalName = SafeString(row, "portal_name"),
                    MessageHead = SafeString(row, "message_head"),
                    MessageBody = SafeString(row, "message_body"),
                    ValidityInMinutes = SafeNullableShort(row, "validity_in_minutes", 60),
                    photo1path = SafeString(row, "photo1path"),
                    photo2path = SafeString(row, "photo2path"),
                    videopath = SafeString(row, "videopath"),
                    documentpath = SafeString(row, "documentpath"),
                    youtubeurl = SafeString(row, "youtubeurl"),
                    facebookurl = SafeString(row, "facebookurl"),
                    instagramurl = SafeString(row, "instagramurl"),
                    Xurl = SafeString(row, "Xurl"),
                    Interest_ID = SafeString(row, "interest_id"),
                    IsSynced = "Yes",
                    Synced = "Yes",
                    SyncDate = DateTime.UtcNow,
                    EntryDate = SafeNullableDateTime(row, "entrydate")
                });
            }

            // Immediately mark as synced in tbl_Message_log
            if (logIds.Count > 0)
            {
                SqlParameter[] syncParams =
                {
                    new SqlParameter("@p_log_ids", string.Join(",", logIds)),
                    new SqlParameter("@p_device_id", DeviceID ?? string.Empty)
                };
                _dataAccess.ExecuteProcedure("sp_mark_message_log_synced", syncParams);
            }

            return Task.FromResult(list);
        }

        public Task<long> CreateNotificationAsync(string deviceID, string? msg_id)
        {
            long? parsedMsgId = null;
            if (!string.IsNullOrWhiteSpace(msg_id) && long.TryParse(msg_id, out long idVal))
            {
                parsedMsgId = idVal;
            }

            SqlParameter[] parameters =
            {
                new SqlParameter("@p_device_id", SqlDbType.NVarChar, 255) { Value = deviceID ?? string.Empty },
                new SqlParameter("@p_msg_id", (object?)parsedMsgId ?? DBNull.Value)
            };

            DataTable dt = _dataAccess.GetDataTableWithProc("sp_create_message_log", parameters);
            long id = 0;
            if (dt.Rows.Count > 0 && dt.Rows[0]["msg_log_id"] != DBNull.Value)
            {
                id = Convert.ToInt64(dt.Rows[0]["msg_log_id"]);
            }
            return Task.FromResult(id);
        }

        public Task<int> MarkMessagesAsSyncedAsync(List<long> notificationIds, string? deviceId = null)
        {
            if (notificationIds == null || notificationIds.Count == 0)
                return Task.FromResult(0);

            var validIds = notificationIds.Where(id => id > 0).Distinct().ToList();
            if (validIds.Count == 0)
                return Task.FromResult(0);

            SqlParameter[] parameters =
            {
                new SqlParameter("@p_log_ids", string.Join(",", validIds)),
                new SqlParameter("@p_device_id", string.IsNullOrWhiteSpace(deviceId) ? (object)DBNull.Value : deviceId.Trim())
            };

            int rows = _dataAccess.ExecuteProcedure("sp_mark_message_log_synced", parameters);
            return Task.FromResult(rows);
        }

        public Task<List<PersonalizedMessageDto>> GetMessageHistoryAsync(string DeviceID, List<string>? interestIds = null, long? portalId = null)
        {
            var list = new List<PersonalizedMessageDto>();

            var validIds = new List<long>();
            if (interestIds != null && interestIds.Count > 0)
            {
                foreach (var id in interestIds)
                {
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    foreach (var part in id.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (long.TryParse(part, out long num) && !validIds.Contains(num))
                        {
                            validIds.Add(num);
                        }
                    }
                }
            }

            SqlParameter[] parameters =
            {
                new SqlParameter("@p_device_id", SqlDbType.NVarChar, 255) { Value = DeviceID ?? string.Empty },
                new SqlParameter("@p_interest_ids", validIds.Count > 0 ? (object)string.Join(",", validIds) : DBNull.Value),
                new SqlParameter("@p_portal_id", (portalId.HasValue && portalId.Value > 0) ? (object)portalId.Value : DBNull.Value)
            };

            DataTable dt = _dataAccess.GetDataTableWithProc("sp_get_message_history", parameters);



            foreach (DataRow row in dt.Rows)
            {
                list.Add(new PersonalizedMessageDto
                {
                    msg_id = SafeLong(row, "msg_id"),
                    msg_log_id = SafeNullableLong(row, "msg_log_id"),
                    DeviceID = SafeString(row, "device_id") ?? DeviceID ?? string.Empty,
                    PortalId = SafeNullableLong(row, "portal_id"),
                    PortalName = SafeString(row, "portal_name"),
                    MessageHead = SafeString(row, "message_head"),
                    MessageBody = SafeString(row, "message_body"),
                    ValidityInMinutes = SafeNullableShort(row, "validity_in_minutes", 60),
                    photo1path = SafeString(row, "photo1path"),
                    photo2path = SafeString(row, "photo2path"),
                    videopath = SafeString(row, "videopath"),
                    documentpath = SafeString(row, "documentpath"),
                    youtubeurl = SafeString(row, "youtubeurl"),
                    facebookurl = SafeString(row, "facebookurl"),
                    instagramurl = SafeString(row, "instagramurl"),
                    Xurl = SafeString(row, "Xurl"),
                    Interest_ID = SafeString(row, "interest_id"),
                    IsDeleted = SafeString(row, "is_deleted") ?? "N",
                    IsSynced = SafeString(row, "is_synced") ?? "No",
                    Synced = SafeString(row, "is_synced") ?? "No",
                    SyncDate = SafeNullableDateTime(row, "sync_date"),
                    EntryDate = SafeNullableDateTime(row, "entrydate")
                });
            }

            return Task.FromResult(list);
        }

        public Task<PersonalizedMessageDto?> GetMessageByIdAsync(long msgId)
        {
            SqlParameter[] parameters = { new SqlParameter("@p_msg_id", SqlDbType.BigInt) { Value = msgId } };
            DataTable dt = _dataAccess.GetDataTableWithProc("sp_get_message_by_id", parameters);
            if (dt.Rows.Count == 0) return Task.FromResult<PersonalizedMessageDto?>(null);

            DataRow row = dt.Rows[0];
            var dto = new PersonalizedMessageDto
            {
                msg_id = SafeLong(row, "msg_id"),
                PortalId = SafeNullableLong(row, "portal_id"),
                PortalName = SafeString(row, "portal_name"),
                MessageHead = SafeString(row, "message_head"),
                MessageBody = SafeString(row, "message_body"),
                ValidityInMinutes = SafeNullableShort(row, "validity_in_minutes", 60),
                photo1path = SafeString(row, "photo1path"),
                photo2path = SafeString(row, "photo2path"),
                videopath = SafeString(row, "videopath"),
                documentpath = SafeString(row, "documentpath"),
                youtubeurl = SafeString(row, "youtubeurl"),
                facebookurl = SafeString(row, "facebookurl"),
                instagramurl = SafeString(row, "instagramurl"),
                Xurl = SafeString(row, "Xurl"),
                Interest_ID = SafeString(row, "interest_id"),
                EntryDate = SafeNullableDateTime(row, "entrydate")
            };

            return Task.FromResult<PersonalizedMessageDto?>(dto);
        }

        public Task<(bool hasExistingLog, int unsyncedCount)> CheckBroadcastSyncStatusAsync(string deviceId, List<string>? interestIds = null, long? portalId = null)
        {
            var validIds = new List<long>();
            if (interestIds != null && interestIds.Count > 0)
            {
                foreach (var id in interestIds)
                {
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    foreach (var part in id.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (long.TryParse(part, out long num) && !validIds.Contains(num))
                        {
                            validIds.Add(num);
                        }
                    }
                }
            }

            string query;
            SqlParameter[] parameters;

            if (validIds.Count > 0)
            {
                string idsStr = string.Join(",", validIds);
                query = @"SELECT  
                        (SELECT COUNT(1) FROM dbo.tbl_Message_log ml INNER JOIN dbo.tbl_Message m ON ml.msg_id = m.msg_id INNER JOIN 
                         STRING_SPLIT(@p_interest_ids, ',') s ON LTRIM(RTRIM(s.value)) = CAST(m.interest_id AS VARCHAR(50))
                         WHERE ml.device_id = @p_device_id
                         AND (@p_portal_id IS NULL OR @p_portal_id = 0 OR m.portal_id = @p_portal_id)) AS ExistingLogCount,
                        (SELECT COUNT(1) FROM dbo.tbl_Message m INNER JOIN STRING_SPLIT(@p_interest_ids, ',') s ON LTRIM(RTRIM(s.value)) = CAST(m.interest_id AS VARCHAR(50))
                         WHERE (m.is_active IS NULL OR m.is_active = 'Y' OR m.is_active = '1')
                         AND (@p_portal_id IS NULL OR @p_portal_id = 0 OR m.portal_id = @p_portal_id)
                         AND NOT EXISTS (SELECT 1 FROM dbo.tbl_Message_log ml2 WHERE ml2.device_id = @p_device_id AND ml2.msg_id = m.msg_id)) AS UnsyncedCount;";

                parameters = new SqlParameter[]
                {
                    new SqlParameter("@p_device_id", SqlDbType.NVarChar, 255) { Value = deviceId ?? string.Empty },
                    new SqlParameter("@p_interest_ids", SqlDbType.NVarChar) { Value = idsStr },
                    new SqlParameter("@p_portal_id", (portalId.HasValue && portalId.Value > 0) ? (object)portalId.Value : DBNull.Value)
                };
            }
            else
            {
                query = @"SELECT  COUNT(1) AS ExistingLogCount, 0 AS UnsyncedCount FROM dbo.tbl_Message_log ml WHERE ml.device_id = @p_device_id;";

                parameters = new SqlParameter[]
                {
                    new SqlParameter("@p_device_id", SqlDbType.NVarChar, 255) { Value = deviceId ?? string.Empty }
                };
            }

            DataTable dt = _dataAccess.GetDataTable(query, parameters);
            int existingCount = 0;
            int unsyncedCount = 0;

            if (dt.Rows.Count > 0)
            {
                existingCount = dt.Rows[0]["ExistingLogCount"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["ExistingLogCount"]) : 0;
                unsyncedCount = dt.Rows[0]["UnsyncedCount"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["UnsyncedCount"]) : 0;
            }

            return Task.FromResult((existingCount > 0, unsyncedCount));
        }

        public Task<List<BroadcastMessageDto>> GetBroadcastMessagesAsync()
        {
            var list = new List<BroadcastMessageDto>();

            const string query = @"SELECT Bmsg_id, message_head, message_body, validity_in_minutes, is_active, photo1path, photo2path, videopath, documentpath, entryby, entrydate, updatedby, updateddate, IsSynced, IsDeleted
                                   FROM dbo.tbl_BroadCastMessage
                                   WHERE (is_active IS NULL OR is_active = 'Y' OR is_active = '1')
                                   ORDER BY entrydate DESC, Bmsg_id DESC;";

            DataTable dt = _dataAccess.GetDataTable(query);

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new BroadcastMessageDto
                {
                    Bmsg_id = SafeLong(row, "Bmsg_id"),
                    MessageHead = SafeString(row, "message_head"),
                    MessageBody = SafeString(row, "message_body"),
                    ValidityInMinutes = SafeNullableShort(row, "validity_in_minutes", 60),
                    IsActive = SafeString(row, "is_active") ?? "Y",
                    photo1path = SafeString(row, "photo1path"),
                    photo2path = SafeString(row, "photo2path"),
                    videopath = SafeString(row, "videopath"),
                    documentpath = SafeString(row, "documentpath"),
                    EntryBy = SafeString(row, "entryby"),
                    EntryDate = SafeNullableDateTime(row, "entrydate"),
                    UpdatedBy = SafeString(row, "updatedby"),
                    UpdatedDate = SafeNullableDateTime(row, "updateddate"),
                    IsSynced = SafeString(row, "IsSynced") ?? "N",
                    IsDeleted = SafeString(row, "IsDeleted") ?? "N"
                });
            }

            return Task.FromResult(list);
        }

        public Task<long> InsertBroadcastMessageAsync(InsertBroadcastMessageRequest request)
        {
            const string insertSql = @"INSERT INTO dbo.tbl_BroadCastMessage (message_head, message_body, validity_in_minutes, is_active, photo1path, photo2path, videopath, documentpath, entryby, entrydate, IsSynced, IsDeleted)
                                       OUTPUT INSERTED.Bmsg_id
                                       VALUES (@message_head, @message_body, @validity_in_minutes, @is_active, @photo1path, @photo2path, @videopath, @documentpath, @entryby, GETDATE(), 'N','N');";

            string activeVal = string.IsNullOrWhiteSpace(request.IsActive) ? "Y" : request.IsActive.Trim().Substring(0, 1).ToUpperInvariant();

            SqlParameter[] parameters =
            {
                new SqlParameter("@message_head", SqlDbType.NVarChar, 255)
                {
                    Value = string.IsNullOrWhiteSpace(request.MessageHead) ? (object)DBNull.Value : request.MessageHead.Trim()
                },
                new SqlParameter("@message_body", SqlDbType.NVarChar, -1)
                {
                    Value = request.MessageBody?.Trim() ?? string.Empty
                },
                new SqlParameter("@validity_in_minutes", SqlDbType.SmallInt)
                {
                    Value = request.ValidityInMinutes.HasValue ? (object)request.ValidityInMinutes.Value : 60
                },
                new SqlParameter("@is_active", SqlDbType.Char, 1)
                {
                    Value = activeVal
                },
                new SqlParameter("@photo1path", SqlDbType.NVarChar, -1)
                {
                    Value = string.IsNullOrWhiteSpace(request.Photo1Path) ? (object)DBNull.Value : request.Photo1Path.Trim()
                },
                new SqlParameter("@photo2path", SqlDbType.NVarChar, -1)
                {
                    Value = string.IsNullOrWhiteSpace(request.Photo2Path) ? (object)DBNull.Value : request.Photo2Path.Trim()
                },
                new SqlParameter("@videopath", SqlDbType.NVarChar, -1)
                {
                    Value = string.IsNullOrWhiteSpace(request.VideoPath) ? (object)DBNull.Value : request.VideoPath.Trim()
                },
                new SqlParameter("@documentpath", SqlDbType.NVarChar, -1)
                {
                    Value = string.IsNullOrWhiteSpace(request.DocumentPath) ? (object)DBNull.Value : request.DocumentPath.Trim()
                },
                new SqlParameter("@entryby", SqlDbType.NVarChar, 255)
                {
                    Value = string.IsNullOrWhiteSpace(request.EntryBy) ? "Admin" : request.EntryBy.Trim()
                }
            };

            DataTable dt = _dataAccess.GetDataTable(insertSql, parameters);
            long bmsgId = 0;
            if (dt != null && dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
            {
                bmsgId = Convert.ToInt64(dt.Rows[0][0]);
            }

            return Task.FromResult(bmsgId);
        }

        public Task<BroadcastMessageDto?> GetBroadcastMessageByIdAsync(long bmsgId)
        {
            const string query = @"SELECT Bmsg_id, message_head, message_body, validity_in_minutes, is_active, photo1path, photo2path, videopath, documentpath, entryby, entrydate, updatedby, updateddate, IsSynced, IsDeleted
                                   FROM dbo.tbl_BroadCastMessage
                                   WHERE Bmsg_id = @bmsg_id;";

            SqlParameter[] parameters =
            {
                new SqlParameter("@bmsg_id", SqlDbType.BigInt) { Value = bmsgId }
            };

            DataTable dt = _dataAccess.GetDataTable(query, parameters);
            if (dt == null || dt.Rows.Count == 0)
            {
                return Task.FromResult<BroadcastMessageDto?>(null);
            }

            DataRow row = dt.Rows[0];
            var dto = new BroadcastMessageDto
            {
                Bmsg_id = SafeLong(row, "Bmsg_id"),
                MessageHead = SafeString(row, "message_head"),
                MessageBody = SafeString(row, "message_body"),
                ValidityInMinutes = SafeNullableShort(row, "validity_in_minutes", 60),
                IsActive = SafeString(row, "is_active") ?? "Y",
                photo1path = SafeString(row, "photo1path"),
                photo2path = SafeString(row, "photo2path"),
                videopath = SafeString(row, "videopath"),
                documentpath = SafeString(row, "documentpath"),
                EntryBy = SafeString(row, "entryby"),
                EntryDate = SafeNullableDateTime(row, "entrydate"),
                UpdatedBy = SafeString(row, "updatedby"),
                UpdatedDate = SafeNullableDateTime(row, "updateddate"),
                IsSynced = SafeString(row, "IsSynced") ?? "N",
                IsDeleted = SafeString(row, "IsDeleted") ?? "N"
            };

            return Task.FromResult<BroadcastMessageDto?>(dto);
        }

        public Task<long> InsertInterestWiseMessageAsync(InsertInterestWiseMessageRequest request)
        {
            const string insertSql = @"INSERT INTO dbo.tbl_Message (
                                            message_head,
                                            message_body,
                                            validity_in_minutes,
                                            is_active,
                                            interest_id,
                                            photo1path,
                                            photo2path,
                                            videopath,
                                            documentpath,
                                            youtubeurl,
                                            facebookurl,
                                            instagramurl,
                                            Xurl,
                                            entryby,
                                            entrydate,
                                            IsDeleted
                                       )
                                       OUTPUT INSERTED.msg_id
                                       VALUES (
                                            @message_head,
                                            @message_body,
                                            @validity_in_minutes,
                                            @is_active,
                                            @interest_id,
                                            @photo1path,
                                            @photo2path,
                                            @videopath,
                                            @documentpath,
                                            @youtubeurl,
                                            @facebookurl,
                                            @instagramurl,
                                            @Xurl,
                                            @entryby,
                                            GETDATE(),
                                            'N'
                                       );";

            string activeVal = string.IsNullOrWhiteSpace(request.IsActive) ? "Y" : request.IsActive.Trim().Substring(0, 1).ToUpperInvariant();

            SqlParameter[] parameters =
            {
                new SqlParameter("@message_head", SqlDbType.NVarChar, 255)
                {
                    Value = string.IsNullOrWhiteSpace(request.MessageHead) ? (object)DBNull.Value : request.MessageHead.Trim()
                },
                new SqlParameter("@message_body", SqlDbType.NVarChar, -1)
                {
                    Value = request.MessageBody?.Trim() ?? string.Empty
                },
                new SqlParameter("@validity_in_minutes", SqlDbType.SmallInt)
                {
                    Value = request.ValidityInMinutes.HasValue ? (object)request.ValidityInMinutes.Value : 60
                },
                new SqlParameter("@is_active", SqlDbType.Char, 1)
                {
                    Value = activeVal
                },
                new SqlParameter("@interest_id", SqlDbType.BigInt)
                {
                    Value = request.InterestId
                },
                new SqlParameter("@photo1path", SqlDbType.NVarChar, -1)
                {
                    Value = string.IsNullOrWhiteSpace(request.Photo1Path) ? (object)DBNull.Value : request.Photo1Path.Trim()
                },
                new SqlParameter("@photo2path", SqlDbType.NVarChar, -1)
                {
                    Value = string.IsNullOrWhiteSpace(request.Photo2Path) ? (object)DBNull.Value : request.Photo2Path.Trim()
                },
                new SqlParameter("@videopath", SqlDbType.NVarChar, -1)
                {
                    Value = string.IsNullOrWhiteSpace(request.VideoPath) ? (object)DBNull.Value : request.VideoPath.Trim()
                },
                new SqlParameter("@documentpath", SqlDbType.NVarChar, -1)
                {
                    Value = string.IsNullOrWhiteSpace(request.DocumentPath) ? (object)DBNull.Value : request.DocumentPath.Trim()
                },
                new SqlParameter("@youtubeurl", SqlDbType.NVarChar, -1)
                {
                    Value = string.IsNullOrWhiteSpace(request.YoutubeUrl) ? (object)DBNull.Value : request.YoutubeUrl.Trim()
                },
                new SqlParameter("@facebookurl", SqlDbType.NVarChar, -1)
                {
                    Value = string.IsNullOrWhiteSpace(request.FacebookUrl) ? (object)DBNull.Value : request.FacebookUrl.Trim()
                },
                new SqlParameter("@instagramurl", SqlDbType.NVarChar, -1)
                {
                    Value = string.IsNullOrWhiteSpace(request.InstagramUrl) ? (object)DBNull.Value : request.InstagramUrl.Trim()
                },
                new SqlParameter("@Xurl", SqlDbType.NVarChar, -1)
                {
                    Value = string.IsNullOrWhiteSpace(request.XUrl) ? (object)DBNull.Value : request.XUrl.Trim()
                },
                new SqlParameter("@entryby", SqlDbType.NVarChar, 255)
                {
                    Value = string.IsNullOrWhiteSpace(request.EntryBy) ? "Admin" : request.EntryBy.Trim()
                }
            };

            DataTable dt = _dataAccess.GetDataTable(insertSql, parameters);
            long msgId = 0;
            if (dt != null && dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
            {
                msgId = Convert.ToInt64(dt.Rows[0][0]);
            }

            return Task.FromResult(msgId);
        }

        public Task<List<InterestWiseMessageDto>> GetInterestWiseMessagesAsync(long? interestId = null)
        {
            string query = @"SELECT m.msg_id,
                                    m.interest_id,
                                    i.interest_name,
                                    m.message_head,
                                    m.message_body,
                                    m.validity_in_minutes,
                                    m.is_active,
                                    m.photo1path,
                                    m.photo2path,
                                    m.videopath,
                                    m.documentpath,
                                    m.youtubeurl,
                                    m.facebookurl,
                                    m.instagramurl,
                                    m.Xurl,
                                    m.entryby,
                                    m.entrydate,
                                    m.IsDeleted
                             FROM dbo.tbl_Message m
                             LEFT JOIN dbo.mst_interest i ON m.interest_id = i.interest_id
                             WHERE m.interest_id IS NOT NULL
                               AND ISNULL(m.IsDeleted, 'N') <> 'Y' "
                               + (interestId.HasValue ? " AND m.interest_id = @interest_id " : "")
                               + " ORDER BY m.msg_id DESC;";

            SqlParameter[] parameters = interestId.HasValue
                ? new[] { new SqlParameter("@interest_id", SqlDbType.BigInt) { Value = interestId.Value } }
                : Array.Empty<SqlParameter>();

            DataTable dt = _dataAccess.GetDataTable(query, parameters);
            var list = new List<InterestWiseMessageDto>();

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new InterestWiseMessageDto
                    {
                        MsgId = SafeLong(row, "msg_id"),
                        InterestId = SafeNullableLong(row, "interest_id"),
                        InterestName = SafeString(row, "interest_name"),
                        MessageHead = SafeString(row, "message_head"),
                        MessageBody = SafeString(row, "message_body"),
                        ValidityInMinutes = SafeNullableShort(row, "validity_in_minutes", 60),
                        IsActive = SafeString(row, "is_active") ?? "Y",
                        Photo1Path = SafeString(row, "photo1path"),
                        Photo2Path = SafeString(row, "photo2path"),
                        VideoPath = SafeString(row, "videopath"),
                        DocumentPath = SafeString(row, "documentpath"),
                        YoutubeUrl = SafeString(row, "youtubeurl"),
                        FacebookUrl = SafeString(row, "facebookurl"),
                        InstagramUrl = SafeString(row, "instagramurl"),
                        XUrl = SafeString(row, "Xurl"),
                        EntryBy = SafeString(row, "entryby"),
                        EntryDate = SafeNullableDateTime(row, "entrydate"),
                        IsDeleted = SafeString(row, "IsDeleted") ?? "N"
                    });
                }
            }

            return Task.FromResult(list);
        }

        private static long SafeLong(DataRow row, string colName, long defaultVal = 0)
        {
            if (!row.Table.Columns.Contains(colName)) return defaultVal;
            var val = row[colName];
            if (val == null || val == DBNull.Value) return defaultVal;
            return long.TryParse(val.ToString(), out long res) ? res : defaultVal;
        }

        private static long? SafeNullableLong(DataRow row, string colName)
        {
            if (!row.Table.Columns.Contains(colName)) return null;
            var val = row[colName];
            if (val == null || val == DBNull.Value) return null;
            return long.TryParse(val.ToString(), out long res) ? res : null;
        }

        private static short? SafeNullableShort(DataRow row, string colName, short? defaultVal = 60)
        {
            if (!row.Table.Columns.Contains(colName)) return defaultVal;
            var val = row[colName];
            if (val == null || val == DBNull.Value) return defaultVal;
            return short.TryParse(val.ToString(), out short res) ? res : defaultVal;
        }

        private static DateTime? SafeNullableDateTime(DataRow row, string colName)
        {
            if (!row.Table.Columns.Contains(colName)) return null;
            var val = row[colName];
            if (val == null || val == DBNull.Value) return null;
            return DateTime.TryParse(val.ToString(), out DateTime res) ? res : null;
        }

        private static string? SafeString(DataRow row, string colName)
        {
            if (!row.Table.Columns.Contains(colName)) return null;
            var val = row[colName];
            if (val == null || val == DBNull.Value) return null;
            return val.ToString();
        }
    }
}
