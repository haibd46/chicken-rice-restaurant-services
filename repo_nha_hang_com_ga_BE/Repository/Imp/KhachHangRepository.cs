using MongoDB.Driver;
using repo_nha_hang_com_ga_BE.Models.MongoDB;
using AutoMapper;
using repo_nha_hang_com_ga_BE.Models.Common.Models.Respond;
using repo_nha_hang_com_ga_BE.Models.Common.Respond;
using repo_nha_hang_com_ga_BE.Models.Responds.KhachHang;
using MongoDB.Bson;
using repo_nha_hang_com_ga_BE.Models.Requests.KhachHang;
using Microsoft.Extensions.Options;
using repo_nha_hang_com_ga_BE.Models.Common;
using repo_nha_hang_com_ga_BE.Models.Common.Paging;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace repo_nha_hang_com_ga_BE.Repository.Imp;

public class KhachHangRepository : IKhachHangRepository
{
    private readonly IMongoCollection<KhachHang> _collection;
    private readonly IMapper _mapper;

    public KhachHangRepository(IOptions<MongoDbSettings> settings, IMapper mapper)
    {
        var mongoClientSettings = settings.Value;
        var client = new MongoClient(mongoClientSettings.Connection);
        var database = client.GetDatabase(mongoClientSettings.DatabaseName);
        _collection = database.GetCollection<KhachHang>("KhachHang");
        _mapper = mapper;
    }

    public async Task<RespondAPIPaging<List<KhachHangRespond>>> GetAllKhachHangs(RequestSearchKhachHang request)
    {
        try
        {
            var collection = _collection;

            var filter = Builders<KhachHang>.Filter.Empty;
            filter &= Builders<KhachHang>.Filter.Eq(x => x.isDelete, false);

            if (!string.IsNullOrEmpty(request.tenKhachHang))
            {
                var tenKhachHangClean = Regex.Replace(request.tenKhachHang.Trim(), @"\s+", " ");
                filter &= Builders<KhachHang>.Filter.Regex(
                    x => x.tenKhachHang,
                    new BsonRegularExpression($".*{Regex.Escape(tenKhachHangClean)}.*", "i")
                );
            }
            if (!string.IsNullOrEmpty(request.diaChi))
            {
                var diaChiClean = Regex.Replace(request.diaChi.Trim(), @"\s+", " ");
                filter &= Builders<KhachHang>.Filter.Regex(
                    x => x.diaChi,
                    new BsonRegularExpression($".*{Regex.Escape(diaChiClean)}.*", "i")
                );

            }
            if (!string.IsNullOrEmpty(request.email))
            {
                var emailClean = Regex.Replace(request.email.Trim(), @"\s+", " ");
                filter &= Builders<KhachHang>.Filter.Regex(
                    x => x.email,
                    new BsonRegularExpression($".*{Regex.Escape(emailClean)}.*", "i")
                );

            }
            if (!string.IsNullOrEmpty(request.soDienThoai))
            {
                var soDienThoaiClean = Regex.Replace(request.soDienThoai.Trim(), @"\s+", " ");
                filter &= Builders<KhachHang>.Filter.Regex(
                    x => x.soDienThoai,
                    new BsonRegularExpression($".*{Regex.Escape(soDienThoaiClean)}.*", "i")
                );

            }

            var projection = Builders<KhachHang>.Projection
                .Include(x => x.tenKhachHang)
                .Include(x => x.diaChi)
                .Include(x => x.email)
                .Include(x => x.soDienThoai);

            var findOptions = new FindOptions<KhachHang, KhachHangRespond>
            {
                Projection = projection
            };

            findOptions.Sort = Builders<KhachHang>.Sort.Descending(x => x.createdDate);

            if (request.IsPaging)
            {
                long totalRecord = await collection.CountDocumentsAsync(filter);
                if (totalRecord <= 0)
                {
                    return new RespondAPIPaging<List<KhachHangRespond>>(
                        ResultRespond.Succeeded,
                        data: new PagingResponse<List<KhachHangRespond>>
                        {
                            Data = new List<KhachHangRespond>(),
                            Paging = new PagingDetail(1, request.PageSize, totalRecord)
                        }
                    );
                }

                int totalPages = (int)Math.Ceiling((double)totalRecord / request.PageSize);

                int currentPage = request.PageNumber;
                if (currentPage < 1) currentPage = 1;
                if (currentPage > totalPages) currentPage = totalPages;

                findOptions.Skip = (currentPage - 1) * request.PageSize;
                findOptions.Limit = request.PageSize;

                var cursor = await collection.FindAsync(filter, findOptions);
                var khachHangs = await cursor.ToListAsync();

                var pagingDetail = new PagingDetail(currentPage, request.PageSize, totalRecord);
                var pagingResponse = new PagingResponse<List<KhachHangRespond>>
                {
                    Paging = pagingDetail,
                    Data = khachHangs
                };

                return new RespondAPIPaging<List<KhachHangRespond>>(
                    ResultRespond.Succeeded,
                    data: pagingResponse
                );
            }
            else
            {
                var cursor = await collection.FindAsync(filter, findOptions);
                var khachHangs = await cursor.ToListAsync();

                return new RespondAPIPaging<List<KhachHangRespond>>(
                    ResultRespond.Succeeded,
                    data: new PagingResponse<List<KhachHangRespond>>
                    {
                        Data = khachHangs,
                        Paging = new PagingDetail(1, khachHangs.Count, khachHangs.Count)
                    }
                );
            }
        }
        catch (Exception ex)
        {
            return new RespondAPIPaging<List<KhachHangRespond>>(
                ResultRespond.Failed,
                message: ex.Message
            );
        }
    }

    public async Task<RespondAPI<KhachHangRespond>> GetKhachHangById(string id)
    {
        try
        {
            var khachHang = await _collection.Find(x => x.Id == id && x.isDelete == false).FirstOrDefaultAsync();

            if (khachHang == null)
            {
                return new RespondAPI<KhachHangRespond>(
                    ResultRespond.Failed,
                    message: "Khách hàng không tồn tại"
                );
            }

            var khachHangRespond = _mapper.Map<KhachHangRespond>(khachHang);

            return new RespondAPI<KhachHangRespond>(
                ResultRespond.Succeeded,
                "Lấy thông tin khách hàng thành công",
                khachHangRespond
            );
        }
        catch (Exception ex)
        {
            return new RespondAPI<KhachHangRespond>(
                ResultRespond.Error,
                $"Đã xảy ra lỗi: {ex.Message}"
            );
        }
    }

    public async Task<RespondAPI<KhachHangRespond>> CreateKhachHang(RequestAddKhachHang request)
    {
        try
        {
            KhachHang newKhachHang = _mapper.Map<KhachHang>(request);

            newKhachHang.isDelete = false;
            newKhachHang.createdDate = DateTime.Now;
            newKhachHang.updatedDate = DateTime.Now;

            await _collection.InsertOneAsync(newKhachHang);

            var khachHangRespond = _mapper.Map<KhachHangRespond>(newKhachHang);

            return new RespondAPI<KhachHangRespond>(
                ResultRespond.Succeeded,
                "Thêm khách hàng thành công",
                khachHangRespond
            );
        }
        catch (Exception ex)
        {
            return new RespondAPI<KhachHangRespond>(
                ResultRespond.Error,
                $"Đã xảy ra lỗi: {ex.Message}"
            );
        }
    }

    public async Task<RespondAPI<KhachHangRespond>> UpdateKhachHang(string id, RequestUpdateKhachHang request)
    {
        try
        {
            var filter = Builders<KhachHang>.Filter.Eq(x => x.Id, id);
            filter &= Builders<KhachHang>.Filter.Eq(x => x.isDelete, false);
            var khachHang = await _collection.Find(filter).FirstOrDefaultAsync();

            if (khachHang == null)
            {
                return new RespondAPI<KhachHangRespond>(
                    ResultRespond.NotFound,
                    "Khách hàng không tồn tại."
                );
            }

            _mapper.Map(request, khachHang);

            khachHang.updatedDate = DateTimeOffset.UtcNow;

            var updateResult = await _collection.ReplaceOneAsync(filter, khachHang);

            if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
            {
                return new RespondAPI<KhachHangRespond>(
                    ResultRespond.Error,
                    "Cập nhật khách hàng không thành công."
                );
            }
            var khachHangRespond = _mapper.Map<KhachHangRespond>(khachHang);

            return new RespondAPI<KhachHangRespond>(
                ResultRespond.Succeeded,
                "Cập nhật khách hàng thành công",
                khachHangRespond
            );
        }
        catch (Exception ex)
        {
            return new RespondAPI<KhachHangRespond>
            (
                ResultRespond.Error,
                $"Đã xảy ra lỗi khi cập nhật thông tin khách hàng: {ex.Message}"
            );
        }
    }

    public async Task<RespondAPI<string>> DeleteKhachHang(string id)
    {
        try
        {
            var existingKhachHang = await _collection.Find(x => x.Id == id && x.isDelete == false).FirstOrDefaultAsync();

            if (existingKhachHang == null)
            {
                return new RespondAPI<string>(
                    ResultRespond.NotFound,
                    message: "Khách hàng không tồn tại"
                );
            }

            var deleteResult = await _collection.DeleteOneAsync(x => x.Id == id);

            if (deleteResult.DeletedCount == 0)
            {
                return new RespondAPI<string>(
                    ResultRespond.Error,
                    "Xóa khách hàng không thành công."
                );
            }
            return new RespondAPI<string>(
                ResultRespond.Succeeded,
                "Xóa khách hàng thành công.",
                id
            );
        }
        catch (Exception ex)
        {
            return new RespondAPI<string>(
                ResultRespond.Error,
                $"Đã xảy ra lỗi khi xóa khách hàng: {ex.Message}"
            );
        }
    }
}