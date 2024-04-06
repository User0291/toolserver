using MySql.Data.MySqlClient;

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
    }
}
