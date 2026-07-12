const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const root = 'C:\\GithubProjeler\\TheyKeepComing\\IncremantalDots';
  const htmlPath = path.join(root, 'tmp', 'dead_walls_blueprint', 'blueprint_render.html');
  const pdfPath = path.join(root, 'tmp', 'dead_walls_blueprint', 'DEAD_WALLS_GAME_DESIGN_BLUEPRINT_v1.0.pdf');
  const browser = await chromium.launch({
    executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    headless: true,
  });
  const page = await browser.newPage({ viewport: { width: 1600, height: 1200 }, deviceScaleFactor: 1 });
  await page.goto('file:///' + htmlPath.replace(/\\/g, '/'), { waitUntil: 'load' });
  await page.emulateMedia({ media: 'print' });
  const metrics = await page.evaluate(() => Array.from(document.querySelectorAll('.page')).map((p, i) => {
    const main = p.querySelector('main');
    return {
      page: i + 1,
      scrollHeight: main.scrollHeight,
      clientHeight: main.clientHeight,
      overflow: main.scrollHeight - main.clientHeight,
      text: main.innerText.slice(0, 80).replace(/\n/g, ' '),
    };
  }));
  console.log(JSON.stringify(metrics.filter(m => m.overflow > 1), null, 2));
  await page.pdf({
    path: pdfPath,
    width: '8.5in',
    height: '11in',
    printBackground: true,
    preferCSSPageSize: true,
    margin: { top: '0', right: '0', bottom: '0', left: '0' },
  });
  await browser.close();
  console.log(pdfPath);
})();
