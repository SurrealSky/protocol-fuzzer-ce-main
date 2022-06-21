from waflib.Build import BuildContext, InstallContext
from waflib.Configure import ConfigurationContext
from waflib.Context import Context
from waflib.Task import TaskBase, CRASHED, MISSING
from waflib import Build, Utils, Logs, Errors, Task
import os.path
import sys

# ConfigurationContext
def find_program(self, filename, **kw):
	path_list = kw.get('path_list', None)
	if not path_list:
		path_list = self.env['PATH']
	kw['path_list'] = path_list

	var = kw.get('var', '')
	filename = self.env[var] or filename
	self.env[var] = None

	if not isinstance(filename, list) and os.path.isabs(filename):
		parts = os.path.split(filename)
		kw['exts'] = ''
		kw['path_list'] = [ parts[0] ]
		filename = parts[1]

	return self.base_find_program(filename, **kw)

# Context
def add_to_group(self, tgen, group=None):
	this = getattr(tgen, 'name', None)
	if this:
		for item in self.get_group(group):
			if this == getattr(item, 'name', None):
				raise Errors.WafError('Duplicate tasks detected!\n%r\n%r' % (item, tgen))

	features = set(Utils.to_list(getattr(tgen, 'features', [])))
	available = set(Utils.to_list(tgen.env['supported_features']))
	intersect = features & available

	if intersect == features:
		self.base_add_to_group(tgen, group)
	elif Logs.verbose > 0:
		missing = [ x for x in (features - intersect)]
		Logs.warn('Skipping %r due to missing features: %s' % (tgen.name, missing))

# BuildContext
def exec_command(self, cmd, **kw):
	subprocess = Utils.subprocess
	kw['shell'] = isinstance(cmd, str)

	if 'cwd' in kw:
		if not isinstance(kw['cwd'], str):
			kw['cwd'] = kw['cwd'].abspath()

	msg = self.logger or Logs
	msg.debug('runner: %r' % cmd)
	msg.debug('runner_env: kw=%s' % kw)

	if not self.logger and Logs.verbose > 0:
		kw['stderr'] = kw['stdout'] = None
	else:
		kw['stderr'] = kw['stdout'] = subprocess.PIPE

	try:
		p = subprocess.Popen(cmd, **kw)
		(out, err) = p.communicate()
		ret = p.returncode
	except Exception as e:
		raise Errors.WafError('Execution failure: %s' % str(e), ex=e)

	if out and not isinstance(out, str):
		out = out.decode(sys.stdout.encoding or 'iso8859-1')
	if self.logger and out:
		self.logger.debug('out: %s' % out)
	elif ret and out:
		sys.stdout.write(out)

	if err and not isinstance(err, str):
		err = err.decode(sys.stdout.encoding or 'iso8859-1')
	if self.logger and err:
		self.logger.debug('err: %s' % err)
	elif err:
		sys.stderr.write(err)

	return ret

def colorize(value, color):
	return color + Logs.colors.BOLD + value + Logs.colors.NORMAL

# TaskBase
def format_error(self):
	if Logs.verbose > 0:
		return self.base_format_error()

	msg = getattr(self, 'last_cmd', '')
	name = getattr(self.generator, 'name', '')
	if getattr(self, "err_msg", None):
		return self.err_msg
	elif not self.hasrun:
		return 'task in %r was not executed for some reason' % (name)
	elif self.hasrun == CRASHED:
		try:
			return ' -> task in %r failed (exit status %r)' % (name, self.err_code)
		except AttributeError:
			return ' -> task in %r failed' % (name)
	elif self.hasrun == MISSING:
		return ' -> missing files in %r' % (name)
	else:
		return 'invalid status for task in %r: %r' % (name, desc.hasrun)

# TaskBase
def display(self):
	sep = Logs.colors.NORMAL + ' | '
	master = self.generator.bld.producer

	def cur():
		# the current task position, computed as late as possible
		tmp = -1
		if hasattr(master, 'ready'):
			tmp -= master.ready.qsize()
		return master.processed + tmp

	if self.generator.bld.progress_bar > 0:
		return self.base_display()

	total = master.total
	n = len(str(total))
	fs = '[%%%dd/%%%dd]' % (n, n)
	pct_str = fs % (cur(), total)
	var_str = self.generator.bld.variant
	bld_str = getattr(self.generator, 'name', self.generator.__class__.__name__)
	tsk_str = self.__class__.__name__
	src_str = str([ x.name for x in self.inputs] )
	tgt_str = str([ x.name for x in self.outputs ])

	if isinstance(self, Build.inst):
		return None

	return "%s | %s | %s | %s | %s | %s\n" % (
		colorize(pct_str, Logs.colors.NORMAL),
		colorize(var_str, Logs.colors.YELLOW),
		colorize(bld_str, Logs.colors.RED),
		colorize(tsk_str, Logs.colors.NORMAL),
		colorize(src_str, Logs.colors.CYAN),
		colorize(tgt_str, Logs.colors.GREEN))

# InstallContext
def do_install(self, src, tgt, lbl, **kw):
	bld = self.generator.bld
	if Logs.verbose <= 1 and bld.progress_bar == 0:
		bld.progress_bar = -1;

	ret = self.base_do_install(src, tgt, lbl, **kw)

	if bld.progress_bar != -1 or str(ret) == 'False':
		return ret

	dest = os.path.split(tgt)[1]
	filename = os.path.split(src)[1]
	target = str(os.path.relpath(tgt, os.path.join(bld.srcnode.abspath(), self.env.PREFIX)))

	msg = "%s | %s | %s | %s\n" % (
		colorize(bld.variant, Logs.colors.YELLOW),
		colorize('Install', Logs.colors.NORMAL),
		colorize(filename, Logs.colors.CYAN),
		colorize(target, Logs.colors.GREEN))

	bld.to_log(msg)

TaskBase.base_format_error = TaskBase.format_error
TaskBase.format_error = format_error

TaskBase.base_display = TaskBase.display
TaskBase.display = display

Context.base_exec_command = Context.exec_command
Context.exec_command = exec_command

ConfigurationContext.base_find_program = ConfigurationContext.find_program
ConfigurationContext.find_program = find_program

BuildContext.base_add_to_group = BuildContext.add_to_group
BuildContext.add_to_group = add_to_group

Build.inst.base_do_install = Build.inst.do_install
Build.inst.do_install = do_install

