param([string]$BaseUrl = 'http://localhost:8080')
$ErrorActionPreference = 'Stop'
$script:checks = 0
function Check-Request($Method, $Path, $Token, $Expected) {
    $headers = @{}
    if ($Token) { $headers.Authorization = "Bearer $Token" }
    try {
        $args = @{ Uri = "$BaseUrl/api$Path"; Method = $Method; Headers = $headers; UseBasicParsing = $true }
        if ($Method -ne 'GET') { $args.ContentType = 'application/json'; $args.Body = '{}' }
        $response = Invoke-WebRequest @args
        $status = [int]$response.StatusCode
        $data = $response.Content | ConvertFrom-Json
    } catch {
        if (!$_.Exception.Response) { throw }
        $status = [int]$_.Exception.Response.StatusCode
        $data = $null
    }
    if ($status -ne $Expected) { throw "$Method $Path expected $Expected, received $status" }
    $script:checks++
    return $data
}
function Login($Email, $Password) {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -ContentType application/json -Body (@{email=$Email;matKhau=$Password} | ConvertTo-Json)
    return $response.data.token
}
$manager = Login 'quanly@ktx.edu.vn' 'QuanLy@123'
$accountant = Login 'ketoan@ktx.edu.vn' 'KeToan@123'
$student = Login 'an.le@sv.edu.vn' 'SinhVien@123'
foreach ($path in @('/sinhvien','/phong','/phi','/hopdong','/phananh','/noiquy','/thongke/tong-quan')) {
    $null = Check-Request GET $path $null 401
}
foreach ($path in @('/sinhvien','/phong','/phi','/hopdong','/phananh','/vipham','/thongke/tong-quan','/thongke/phananh-theo-nhom')) {
    $null = Check-Request GET $path $manager 200
}
foreach ($path in @('/phong','/phi','/hopdong','/thongke/tong-quan')) { $null = Check-Request GET $path $accountant 200 }
foreach ($path in @('/sinhvien','/sinhvien/1','/sinhvien/me','/phananh','/phananh/1','/vipham','/thongke/phananh-theo-nhom')) {
    $null = Check-Request GET $path $accountant 403
}
foreach ($path in @('/sinhvien','/sinhvien/2','/phi','/phi/1','/vipham','/thongke/tong-quan')) {
    $null = Check-Request GET $path $student 403
}
foreach ($path in @('/phong','/phi/cua-toi','/hopdong','/phananh','/sinhvien/me','/noiquy')) {
    $null = Check-Request GET $path $student 200
}
foreach ($token in @($accountant, $student)) {
    foreach ($path in @('/phong/1','/sinhvien/1','/giuong/1','/toanha/1','/tang/1','/noiquy/1','/vipham/1','/phi/1','/phananh/1')) {
        $null = Check-Request DELETE $path $token 403
    }
    $null = Check-Request POST '/hopdong/xep-phong' $token 403
    $null = Check-Request PUT '/phananh/1/trang-thai' $token 403
}
$null = Check-Request POST '/phi' $student 403
$null = Check-Request PUT '/phi/1/thanh-toan' $student 403
$null = Check-Request POST '/phananh' $accountant 403
$me = (Check-Request GET '/sinhvien/me' $student 200).data
foreach ($resource in @('hopdong','phananh')) {
    $own = (Check-Request GET "/$resource" $student 200).data.items
    if (@($own | Where-Object { $_.sinhVienId -ne $me.id }).Count) { throw "Student can read another student's $resource" }
    $other = (Check-Request GET "/$resource`?pageSize=100" $manager 200).data.items | Where-Object { $_.sinhVienId -ne $me.id } | Select-Object -First 1
    if (!$other) { throw "Missing cross-owner fixture for $resource" }
    $null = Check-Request GET "/$resource/$($other.id)" $student 404
    $filtered = (Check-Request GET "/$resource`?sinhVienId=$($other.sinhVienId)" $student 200).data.items
    if (@($filtered | Where-Object { $_.sinhVienId -ne $me.id }).Count) { throw 'Student filter bypassed ownership' }
}
$null = Check-Request GET '/phong' 'invalid-token' 401
Write-Output "Passed $script:checks authorization checks. No data modified."
