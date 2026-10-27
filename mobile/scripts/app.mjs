/**
 * npm run app        產出正式簽章 APK，WebView 指向本機網站；手機接上就安裝並打開。
 * npm run app:release  同一把簽章，WebView 指向已部署網站，並複製到 wwwroot/app。
 */
import { copyFileSync, existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

const mobileRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const repoRoot = resolve(mobileRoot, '..');
const androidDir = join(mobileRoot, 'android');
const release = process.argv.includes('--release');
const appId = 'com.midnight.ec.plant';

function readJson(path) {
  return JSON.parse(readFileSync(path, 'utf8'));
}

function firstExisting(paths) {
  for (const p of paths) {
    if (p && existsSync(join(p, 'bin', process.platform === 'win32' ? 'java.exe' : 'java'))) {
      return p;
    }
  }
  return null;
}

function normalizeJavaHome(home) {
  return home.replace(/[\\/]+$/, '');
}

function resolveJavaHome() {
  const fromEnv = process.env.JAVA_HOME?.trim();
  if (fromEnv) {
    try {
      const v = spawnSync(join(fromEnv, 'bin', process.platform === 'win32' ? 'java.exe' : 'java'), ['-version'], {
        encoding: 'utf8',
        windowsHide: true,
      });
      const text = `${v.stderr || ''}${v.stdout || ''}`;
      const m = text.match(/version "(\d+)/);
      if (m && Number(m[1]) <= 20) return normalizeJavaHome(fromEnv);
      if (m) console.warn(`[app] JAVA_HOME is Java ${m[1]}; searching JDK 17…`);
    } catch {
      /* fall through */
    }
  }

  const programFiles = process.env.ProgramFiles || 'C:\\Program Files';
  const candidates = [];
  const msRoot = join(programFiles, 'Microsoft');
  if (existsSync(msRoot)) {
    for (const name of readdirSync(msRoot)) {
      if (/^jdk-17/i.test(name)) candidates.push(join(msRoot, name));
    }
  }
  const adoptium = join(programFiles, 'Eclipse Adoptium');
  if (existsSync(adoptium)) {
    for (const name of readdirSync(adoptium)) {
      if (/jdk-17/i.test(name)) candidates.push(join(adoptium, name));
    }
  }
  candidates.push(join(programFiles, 'Java', 'jdk-17'));
  const found = firstExisting(candidates);
  return found ? normalizeJavaHome(found) : null;
}

function resolveAndroidSdk() {
  return process.env.ANDROID_HOME
    || process.env.ANDROID_SDK_ROOT
    || join(process.env.LOCALAPPDATA || '', 'Android', 'Sdk');
}

function loadConfig() {
  const examplePath = join(mobileRoot, 'app.config.example.json');
  const localPath = join(mobileRoot, 'app.config.local.json');
  const example = readJson(examplePath);
  const local = existsSync(localPath) ? readJson(localPath) : {};
  return { ...example, ...local };
}

function loadSigning() {
  const path = join(mobileRoot, 'signing.local.json');
  if (!existsSync(path)) {
    console.error('缺少 mobile/signing.local.json。複製 mobile/signing.example.json 後填上金鑰路徑與密碼。');
    console.error('金鑰不要提交。建立範例：');
    console.error('  keytool -genkeypair -v -keystore plant-release.jks -alias plant -keyalg RSA -keysize 2048 -validity 10000');
    process.exit(1);
  }
  const signing = readJson(path);
  if (!signing.storeFile || !existsSync(signing.storeFile)) {
    console.error(`找不到簽章檔：${signing.storeFile || '(空白)'}`);
    process.exit(1);
  }
  return signing;
}

function writeCapacitorConfig(serverUrl) {
  const cleartext = serverUrl.startsWith('http://');
  const config = {
    appId,
    appName: '我的植栽',
    webDir: 'www',
    server: {
      url: serverUrl,
      cleartext,
      androidScheme: cleartext ? 'http' : 'https',
    },
    android: { allowMixedContent: cleartext },
  };
  writeFileSync(join(mobileRoot, 'capacitor.config.json'), JSON.stringify(config, null, 2));
}

function quoteCmd(value) {
  const text = String(value);
  if (text.length === 0) return '""';
  if (!/[ \t"&|<>^%]/.test(text)) return text;
  return `"${text.replace(/"/g, '""')}"`;
}

function run(command, args, cwd, env) {
  const isBat = process.platform === 'win32' && /\.(bat|cmd)$/i.test(command);
  const options = {
    cwd,
    stdio: 'inherit',
    env: env || process.env,
  };
  let file = command;
  let argv = args;
  if (isBat) {
    // cmd.exe /s strips one outer pair of quotes, so quote each token ourselves.
    file = process.env.ComSpec || 'cmd.exe';
    argv = ['/d', '/s', '/c', [command, ...args].map(quoteCmd).join(' ')];
    options.windowsVerbatimArguments = true;
  }
  const result = spawnSync(file, argv, options);
  if (result.error) {
    console.error(result.error.message);
    process.exit(1);
  }
  if (result.status !== 0) process.exit(result.status ?? 1);
}

function ensureAndroidProject(env) {
  const cap = join(repoRoot, 'node_modules', '@capacitor', 'cli', 'bin', 'capacitor');
  if (!existsSync(cap)) {
    console.error('尚未安裝 Capacitor。在專案根目錄執行 npm install。');
    process.exit(1);
  }
  if (!existsSync(androidDir)) {
    run(process.execPath, [cap, 'add', 'android'], mobileRoot, env);
  }
  run(process.execPath, [cap, 'sync', 'android'], mobileRoot, env);
}

function copyNativeSources() {
  const javaDir = join(androidDir, 'app', 'src', 'main', 'java', 'com', 'midnight', 'ec', 'plant');
  mkdirSync(javaDir, { recursive: true });
  copyFileSync(join(mobileRoot, 'native', 'MainActivity.java'), join(javaDir, 'MainActivity.java'));
  copyFileSync(join(mobileRoot, 'native', 'ApkUpdatePlugin.java'), join(javaDir, 'ApkUpdatePlugin.java'));
}

function patchVersion(version) {
  const variablesPath = join(androidDir, 'variables.gradle');
  if (!existsSync(variablesPath)) {
    console.error('找不到 android/variables.gradle');
    process.exit(1);
  }
  let text = readFileSync(variablesPath, 'utf8');
  text = text.replace(/versionCode\s*=\s*\d+/, `versionCode = ${version.versionCode}`);
  text = text.replace(/versionName\s*=\s*'[^']*'/, `versionName = '${version.versionName}'`);
  writeFileSync(variablesPath, text);
}

function patchManifest() {
  const manifestPath = join(androidDir, 'app', 'src', 'main', 'AndroidManifest.xml');
  let xml = readFileSync(manifestPath, 'utf8');
  if (!xml.includes('REQUEST_INSTALL_PACKAGES')) {
    xml = xml.replace(
      /<manifest\b[^>]*>/,
      (open) => `${open}\n    <uses-permission android:name="android.permission.REQUEST_INSTALL_PACKAGES" />`);
  }
  if (!xml.includes('usesCleartextTraffic')) {
    xml = xml.replace('<application', '<application android:usesCleartextTraffic="true"');
  }
  writeFileSync(manifestPath, xml);

  const pathsFile = join(androidDir, 'app', 'src', 'main', 'res', 'xml', 'file_paths.xml');
  if (existsSync(pathsFile)) {
    let paths = readFileSync(pathsFile, 'utf8');
    if (!paths.includes('cache-path')) {
      paths = paths.replace('</paths>', '    <cache-path name="apk_cache" path="." />\n</paths>');
      writeFileSync(pathsFile, paths);
    }
  }
}

function assemble(signing, env) {
  const isWin = process.platform === 'win32';
  const gradlew = isWin ? 'gradlew.bat' : './gradlew';
  run(gradlew, [
    ':app:assembleRelease',
    `-Pandroid.injected.signing.store.file=${signing.storeFile}`,
    `-Pandroid.injected.signing.store.password=${signing.storePassword}`,
    `-Pandroid.injected.signing.key.alias=${signing.keyAlias}`,
    `-Pandroid.injected.signing.key.password=${signing.keyPassword}`,
  ], androidDir, env);
}

function apkPath() {
  return join(androidDir, 'app', 'build', 'outputs', 'apk', 'release', 'app-release.apk');
}

function publish(version) {
  const destDir = join(repoRoot, 'Midnight.EC.Plant.WEB', 'wwwroot', 'app');
  mkdirSync(destDir, { recursive: true });
  copyFileSync(apkPath(), join(destDir, 'plant.apk'));
  writeFileSync(join(destDir, 'version.json'), JSON.stringify({
    versionCode: version.versionCode,
    versionName: version.versionName,
    apkUrl: '/app/plant.apk',
  }, null, 2));
  console.log(`[app] 已放到 ${destDir}`);
}

function installIfConnected(sdk) {
  const adbName = process.platform === 'win32' ? 'adb.exe' : 'adb';
  const adb = join(sdk, 'platform-tools', adbName);
  if (!existsSync(adb)) {
    console.warn('[app] 找不到 adb，略過安裝。');
    return;
  }
  const listed = spawnSync(adb, ['devices'], { encoding: 'utf8', windowsHide: true });
  const online = (listed.stdout || '')
    .split(/\r?\n/)
    .some((line) => line.trim().endsWith('device') && !line.startsWith('List'));
  if (!online) {
    console.log('[app] 沒有接上的手機，APK 已產出但未安裝。');
    return;
  }
  run(adb, ['install', '-r', apkPath()], mobileRoot);
  run(adb, ['shell', 'am', 'start', '-n', `${appId}/.MainActivity`], mobileRoot);
}

function main() {
  if (!existsSync(join(mobileRoot, 'app.config.local.json'))) {
    console.error('請複製 mobile/app.config.example.json 為 mobile/app.config.local.json，並填上網址。');
    console.error('localUrl 用這台電腦的區網位址（手機才連得到），publicUrl 用已部署的網站。');
    process.exit(1);
  }
  const config = loadConfig();
  const serverUrl = release ? config.publicUrl : config.localUrl;
  if (!serverUrl || serverUrl.includes('example.com')) {
    console.error(release
      ? '請在 mobile/app.config.local.json 設定 publicUrl 為已部署的網站。'
      : '請在 mobile/app.config.local.json 設定 localUrl。');
    process.exit(1);
  }

  const signing = loadSigning();
  const version = readJson(join(mobileRoot, 'version.json'));
  const javaHome = resolveJavaHome();
  if (!javaHome) {
    console.error('找不到 JDK 17。可安裝：winget install Microsoft.OpenJDK.17');
    process.exit(1);
  }
  const sdk = resolveAndroidSdk();
  const env = { ...process.env, JAVA_HOME: javaHome, ANDROID_HOME: sdk, ANDROID_SDK_ROOT: sdk };
  console.log(`[app] ${release ? 'release' : 'dev'} → ${serverUrl}`);
  console.log(`[app] JAVA_HOME=${javaHome}`);

  writeCapacitorConfig(serverUrl);
  ensureAndroidProject(env);
  copyNativeSources();
  patchVersion(version);
  patchManifest();
  assemble(signing, env);

  if (!existsSync(apkPath())) {
    console.error(`找不到 ${apkPath()}`);
    process.exit(1);
  }
  console.log(`[app] APK → ${apkPath()}`);
  if (release) publish(version);
  else installIfConnected(sdk);
}

main();
