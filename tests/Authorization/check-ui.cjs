const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const source = fs.readFileSync('frontend/js/api.js', 'utf8');
let checks = 0;
for (const role of ['QuanLy', 'KeToan', 'SinhVien']) {
  for (const page of ['/dashboard.html', '/sinhvien.html', '/phananh.html', '/phi.html', '/phong.html', '/chatbot.html']) {
    const paths = ['/dashboard.html', '/sinhvien.html', '/phananh.html', '/phi.html'];
    const links = paths.map(path => ({ hidden: false, getAttribute: () => path }));
    let destination;
    const context = vm.createContext({
      localStorage: { getItem: key => key === 'ktx_token' ? 'token' : JSON.stringify({ vaiTro: role }) },
      window: { location: { pathname: page, replace: value => { destination = value; } } },
      document: { querySelectorAll: () => links, getElementById: () => null },
    });
    vm.runInContext(source, context);
    const forbidden = (page === '/sinhvien.html' && role !== 'QuanLy') ||
      (page === '/dashboard.html' && role === 'SinhVien') ||
      (page === '/phananh.html' && role === 'KeToan');
    if (forbidden) {
      assert.throws(() => vm.runInContext('requireLogin()', context), /Page forbidden/);
      assert.equal(destination, role === 'SinhVien' ? '/phi.html' : '/dashboard.html');
    } else {
      vm.runInContext('requireLogin()', context);
      assert.equal(links[1].hidden, role !== 'QuanLy');
      assert.equal(links[2].hidden, role === 'KeToan');
      assert.equal(links[0].hidden, role === 'SinhVien');
    }
    checks++;
  }
}
console.log(`Passed ${checks} page and navigation permission checks.`);
