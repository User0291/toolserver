using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace MainWeb
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("YourDatabaseConnection");
        }
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task UpdateUserAsync(User user)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // 开启事务
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var query = "UPDATE Users SET DeviceId = @DeviceId, IsUsed = @IsUsed, ExpiryDate = @ExpiryDate WHERE Id = @Id";
                        using (var command = new MySqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@DeviceId", user.DeviceId);
                            command.Parameters.AddWithValue("@IsUsed", user.IsUsed);
                            command.Parameters.AddWithValue("@ExpiryDate", user.ExpiryDate);
                            command.Parameters.AddWithValue("@Id", user.Id);

                            // 执行更新操作
                            await command.ExecuteNonQueryAsync();
                        }

                        // 提交事务
                        await transaction.CommitAsync();
                    }
                    catch (Exception)
                    {
                        // 回滚事务
                        await transaction.RollbackAsync();
                        throw; // 抛出异常以供上层处理
                    }
                }
            }
        }
        /// <summary>
        /// 查詢
        /// </summary>
        /// <param name="licenseKey"></param>
        /// <returns></returns>
        public async Task<User> GetUserByLicenseKeyAsync(string licenseKey)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = "SELECT Id, `Key16`, DeviceId, IsUsed, ExpiryDate FROM Users WHERE `Key16` = @LicenseKey";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@LicenseKey", licenseKey);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                Id = reader.GetInt32(0),
                                Key16 = reader.GetString(1),
                                DeviceId = reader.GetString(2),
                                IsUsed = reader.GetBoolean(3),
                                ExpiryDate = reader.GetDateTime(4)
                            };
                        }
                    }
                }
            }

            return null;
        }
        /// <summary>
        /// 查詢客戶端版本號
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetVersion()
        {
            string version = null;

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = "SELECT *FROM ClientVersion"; // 假设版本号存储在名为 AppVersion 的表中
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            version = reader.GetString(0);
                        }
                    }
                }
            }

            return version;
        }
        /// <summary>
        /// 获取公告
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetNewNoticeAsync()
        {
            string notice = null;

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = "SELECT notice FROM Notice ORDER BY id DESC LIMIT 1"; // 获取最新的一条公告
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            notice = reader.GetString(0);
                        }
                    }
                }
            }

            return notice;
        }
        /// <summary>
        /// 保存qq號碼
        /// </summary>
        /// <param name="qqNumbers"></param>
        /// <returns></returns>
        public async Task SaveQQNumbersToDatabase(List<string>? qqNumbers)
        {
            if (qqNumbers!=null&&qqNumbers.Count>0)
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // 将新的 QQ 号码插入到数据库中
                    string insertQuery = "INSERT INTO QQInfo (qqnumber) VALUES (@QQNumber)";
                    foreach (string qqNumber in qqNumbers)
                    {
                        using (var insertCommand = new MySqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@QQNumber", qqNumber);
                            await insertCommand.ExecuteNonQueryAsync();
                        }
                    }
                }
        }

        /// <summary>
        /// 获取外部公告内容
        /// </summary>
        /// <returns>外部公告内容</returns>
        public async Task<string> GetOpenNotice()
        {
            string openNotice = null;

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = "SELECT opennotice FROM OpenNotice";
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            openNotice = reader.GetString(0);
                        }
                    }
                }
            }

            return openNotice;
        }
    }
}
